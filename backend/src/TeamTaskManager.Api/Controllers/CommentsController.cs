using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamTaskManager.Api.Contracts.Comments;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
// 1. AUTHENTICATION GATE:
// Every endpoint in this controller requires a valid, unexpired JWT bearer token.
// Missing or invalid token -> 401 Unauthorized immediately.
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CommentsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // =========================================================================
    // 1. CREATE COMMENT (POST: /api/tasks/{taskId}/comments)
    // =========================================================================
    // Adds a new discussion comment to a specific task.
    //
    // SECURITY & PERMISSION RULES:
    // - Caller must be authenticated (JWT).
    // - Task must exist (404 if not found).
    // - Resource-Level Auth: Caller must be a member of the project that owns the task (403 if not).
    // - Creator Enforcement: AuthorId is ALWAYS taken from currentUserId (JWT), never request body!
    // =========================================================================
    [HttpPost("api/tasks/{taskId:guid}/comments")]
    public async Task<IActionResult> CreateComment(
        Guid taskId,
        CreateCommentRequest request)
    {
        // 1. EXTRACT CALLER IDENTITY FROM JWT:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. VERIFY TARGET TASK EXISTS:
        var task =
            await _dbContext.TaskItems
                .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        // 3. RESOURCE-LEVEL AUTHORIZATION CHECK (IDOR DEFENSE):
        // Navigate: taskId -> task.ProjectId -> ProjectMember check.
        // A user from Project B cannot comment on tasks in Project A even if they guess the taskId!
        var isProjectMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == task.ProjectId &&
                    x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid(); // Caller does not belong to this project -> 403 Forbidden
        }

        // 4. INSTANTIATE COMMENT WITH SECURE OWNERSHIP:
        // Critical Security Rule: AuthorId = currentUserId from JWT.
        // The client cannot forge who authored the comment.
        var comment = new Comment
        {
            TaskItemId = taskId,
            AuthorId = currentUserId,
            Body = request.Body
        };

        _dbContext.Comments.Add(comment);

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            comment.Id,
            comment.TaskItemId,
            comment.AuthorId,
            comment.Body,
            comment.CreatedAt
        });
    }

    // =========================================================================
    // 2. READ TASK COMMENTS (GET: /api/tasks/{taskId}/comments)
    // =========================================================================
    // Retrieves all discussion comments for a specific task.
    //
    // SECURITY & QUERY OPTIMIZATIONS:
    // - Caller must belong to the parent project to read comments.
    // - .OrderBy(x => x.CreatedAt): Returns comments in chronological order (oldest -> newest).
    // - .Select(...): Shapes the response DTO directly in SQL, avoiding cyclic JSON loops
    //   and loading only the needed author fields (Id, DisplayName, Email).
    // =========================================================================
    [HttpGet("api/tasks/{taskId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid taskId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. VERIFY TASK EXISTS:
        var task = await _dbContext.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId);
        if (task == null)
        {
            return NotFound("Task not found.");
        }

        // 3. RESOURCE-LEVEL ACCESS CHECK:
        // Caller must be an active project member to view this task's discussion thread.
        var isProjectMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == task.ProjectId &&
                    x.UserId == currentUserId);

        if (!isProjectMember)
        {  
            return Forbid(); 
        }

        // 4. EF CORE PROJECTION & CHRONOLOGICAL SORTING:
        // - Where: Filter comments by taskId
        // - OrderBy: Chronological chat stream (first posted to last posted)
        // - Select: Project author profile details without loading entire ApplicationUser entity
        var comments =
            await _dbContext.Comments
                .Where(x => x.TaskItemId == taskId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Body,
                    x.CreatedAt,
                    x.ModifiedAt,

                    Author = new
                    {
                        x.Author!.Id,
                        x.Author.DisplayName,
                        x.Author.Email
                    }
                })
                .ToListAsync();

        return Ok(comments);
    }

    // =========================================================================
    // 3. UPDATE COMMENT (PUT: /api/comments/{commentId})
    // =========================================================================
    // Edits the text body of an existing comment.
    //
    // AUTHORIZATION RULE (AUTHOR ONLY):
    // - Only the original author of the comment may edit its content.
    // - Even Project Owners CANNOT edit another user's comment (preserves discussion integrity).
    // - Non-members and non-authors -> 403 Forbidden.
    // =========================================================================
    [HttpPut("api/comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(
        Guid commentId,
        UpdateCommentRequest request)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET COMMENT WITH EAGER LOADING:
        // .Include(x => x.TaskItem) loads the related task so we can access comment.TaskItem.ProjectId
        // without writing a second separate database query.
        var comment =
            await _dbContext.Comments
                .Include(x => x.TaskItem)
                .FirstOrDefaultAsync(x => x.Id == commentId);

        if (comment == null)
        {
            return NotFound("Comment not found.");
        }

        // 3. PROJECT MEMBERSHIP CHECK:
        // Caller must still be a member of the project where this comment lives.
        var isProjectMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == comment.TaskItem!.ProjectId &&
                    x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        // 4. AUTHOR-ONLY EDIT PERMISSION CHECK:
        // Strict boundary: Only the comment's creator can modify its content.
        if (comment.AuthorId != currentUserId)
        {
            return Forbid(); // Another user attempting to edit this comment -> 403 Forbidden
        }

        // 5. UPDATE BODY & AUDIT TIMESTAMP:
        comment.Body = request.Body;
        comment.ModifiedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            comment.Id,
            comment.Body,
            comment.ModifiedAt
        });
    }

    // =========================================================================
    // 4. DELETE COMMENT (DELETE: /api/comments/{commentId})
    // =========================================================================
    // Deletes an existing comment.
    //
    // AUTHORIZATION POLICY (AUTHOR OR PROJECT OWNER):
    // - Comment Author: Can delete their own comment.
    // - Project Owner: Can delete ANY comment in their project (moderation power).
    // - Regular Member (non-author): CANNOT delete another user's comment -> 403 Forbidden.
    // - Non-Member: 403 Forbidden.
    // =========================================================================
    [HttpDelete("api/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET COMMENT WITH PARENT TASK:
        var comment =
            await _dbContext.Comments
                .Include(x => x.TaskItem)
                .FirstOrDefaultAsync(x => x.Id == commentId);

        if (comment == null)
        {
            return NotFound("Comment not found.");
        }

        // 3. RETRIEVE CALLER'S PROJECT MEMBERSHIP & ROLE:
        // We need the full ProjectMember record to inspect membership.Role.
        var membership =
            await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == comment.TaskItem!.ProjectId &&
                    x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid(); // Caller does not belong to this project -> 403 Forbidden
        }

        // 4. EVALUATE AUTHORIZATION CONDITION:
        // Author can delete their own comment.
        // Project Owner can delete any comment (moderation).
        var isAuthor =
            comment.AuthorId == currentUserId;

        var isOwner =
            membership.Role == ProjectRole.Owner;

        // If caller is neither the author nor the project owner -> Deny!
        if (!isAuthor && !isOwner)
        {
            return Forbid();
        }

        // 5. REMOVE & PERSIST:
        _dbContext.Comments.Remove(comment);

        await _dbContext.SaveChangesAsync();

        // 6. RETURN 204 NO CONTENT:
        return NoContent();
    }
}
