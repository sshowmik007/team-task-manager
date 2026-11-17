using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamTaskManager.Api.Contracts.Tasks;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
// 1. AUTHENTICATION GATE:
// Every endpoint in this controller requires a valid, unexpired JWT bearer token.
// Missing or invalid token -> 401 Unauthorized immediately.
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TasksController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // =========================================================================
    // 1. CREATE TASK (POST: /api/projects/{projectId}/tasks)
    // =========================================================================
    [HttpPost("api/projects/{projectId:guid}/tasks")]
    public async Task<IActionResult> CreateTask(
        Guid projectId,
        CreateTaskRequest request)
    {
        // 1. GET CALLER IDENTITY FROM JWT:
        // Extract the logged-in user ID from the cryptographically verified JWT claim.
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. RESOURCE-LEVEL AUTHORIZATION CHECK:
        // Verify caller is a verified member (Owner or Member) of this specific project.
        // Non-member calling this endpoint -> 403 Forbidden.
        var isProjectMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        // 3. OPTIONAL ASSIGNEE MEMBERSHIP VALIDATION:
        // If AssigneeId is provided, ensure that user actually belongs to THIS project.
        // Prevents assigning tasks to strangers, deleted accounts, or users from other workspaces.
        if (request.AssigneeId.HasValue)
        {
            var assigneeIsMember = await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == projectId &&
                    x.UserId == request.AssigneeId.Value);

            if (!assigneeIsMember)
            {
                return BadRequest("Assignee must be a member of this project.");
            }
        }

        // 4. INSTANTIATE TASK WITH SAFE DEFAULTS:
        // CRITICAL SECURITY RULE: CreatedById = currentUserId
        // We NEVER accept CreatedById from the client JSON (prevents impersonation).
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            CreatedById = currentUserId,       // Enforced from JWT
            Status = TaskItemStatus.Todo       // New tasks always start in Todo
        };

        _dbContext.TaskItems.Add(task);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.ProjectId,
            task.AssigneeId,
            task.CreatedById,
            task.CreatedAt
        });
    }

    // =========================================================================
    // 2. LIST PROJECT TASKS (GET: /api/projects/{projectId}/tasks)
    // =========================================================================
    [HttpGet("api/projects/{projectId:guid}/tasks")]
    public async Task<IActionResult> GetTasks(Guid projectId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 1. PROJECT ACCESS CHECK:
        // Caller must belong to the project before reading any task data.
        var isProjectMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        // 2. EF CORE PROJECTION PIPELINE:
        // TaskItems -> .Where(...) -> .Select(...) -> .ToListAsync()
        // - .Where() filters tasks by project in SQL (WHERE "ProjectId" = @projectId)
        // - .Select() shapes the exact JSON DTO without loading full entity graph or cyclic references
        // - Includes nested Assignee & CreatedBy details cleanly
        var tasks = await _dbContext.TaskItems
            .Where(x => x.ProjectId == projectId)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Description,
                x.Status,
                x.ProjectId,

                Assignee = x.Assignee == null
                    ? null
                    : new
                    {
                        x.Assignee.Id,
                        x.Assignee.DisplayName,
                        x.Assignee.Email
                    },

                CreatedBy = new
                {
                    x.CreatedBy!.Id,
                    x.CreatedBy.DisplayName,
                    x.CreatedBy.Email
                },

                x.CreatedAt,
                x.ModifiedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }

    // =========================================================================
    // 3. GET SINGLE TASK (GET: /api/tasks/{taskId})
    // =========================================================================
    [HttpGet("api/tasks/{taskId:guid}")]
    public async Task<IActionResult> GetTask(Guid taskId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 1. LOOK UP TASK:
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(); // Task does not exist -> 404 Not Found
        }

        // 2. RESOURCE-LEVEL AUTHORIZATION CHECK (IDOR DEFENSE):
        // taskId -> find task -> read task.ProjectId -> verify caller is in ProjectMembers
        // Even if someone guesses a valid taskId GUID from another company/project,
        // this check immediately blocks them with 403 Forbidden!
        var isProjectMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == task.ProjectId &&
                x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        return Ok(new
        {
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.ProjectId,
            task.AssigneeId,
            task.CreatedById,
            task.CreatedAt,
            task.ModifiedAt
        });
    }

    // =========================================================================
    // 4. UPDATE TASK (PUT: /api/tasks/{taskId})
    // =========================================================================
    // Modifies an existing task's mutable fields (Title, Description, Status, AssigneeId).
    //
    // SECURITY & VALIDATION PIPELINE:
    // 1. Authenticate caller via JWT (User.FindFirstValue).
    // 2. Look up task by taskId (404 if not found).
    // 3. IDOR Defense: Verify caller is an active member of task.ProjectId (403 if not).
    // 4. Validate Status enum value (400 if undefined integer passed).
    // 5. Cross-Resource Validation: If AssigneeId provided, ensure assignee is also a member of task.ProjectId (400 if not).
    // 6. Update mutable fields and stamp ModifiedAt with DateTimeOffset.UtcNow.
    // 7. SaveChangesAsync and return updated resource DTO.
    // =========================================================================
    [HttpPut("api/tasks/{taskId:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid taskId,
        UpdateTaskRequest request)
    {
        // 1. GET CALLER IDENTITY FROM JWT:
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET TASK:
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(); // Task does not exist -> 404 Not Found
        }

        // 3. RESOURCE-LEVEL AUTHORIZATION CHECK (IDOR DEFENSE):
        // Retrieve the caller's membership in the task's project.
        // A user cannot edit a task in a project they are not a member of!
        var membership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == task.ProjectId &&
                x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid(); // Caller does not belong to this project -> 403 Forbidden
        }

        // 4. ENUM RANGE VALIDATION:
        // Ensures the client sent a valid TaskItemStatus enum value (Todo=0, InProgress=1, Done=2).
        // Prevents storing out-of-range integer values (e.g., 999) into the database.
        if (!Enum.IsDefined(typeof(TaskItemStatus), request.Status))
        {
            return BadRequest("Invalid task status.");
        }

        // 5. ASSIGNEE INTEGRITY CHECK:
        // If an assignee is specified, verify they belong to THIS project.
        // Prevents assigning tasks to external users or users outside the project boundary.
        if (request.AssigneeId.HasValue)
        {
            var assigneeIsMember = await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == task.ProjectId &&
                    x.UserId == request.AssigneeId.Value);

            if (!assigneeIsMember)
            {
                return BadRequest("Assignee must be a member of this project.");
            }
        }

        // 6. APPLY UPDATES & AUDIT TIMESTAMP:
        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.AssigneeId = request.AssigneeId;
        task.ModifiedAt = DateTimeOffset.UtcNow; // Explicit audit timestamp for when modifications occur

        // 7. PERSIST CHANGES:
        // EF Core Change Tracker detects modified properties on the tracked 'task' entity
        // and emits an optimized SQL UPDATE statement targeting only this row.
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.AssigneeId,
            task.ModifiedAt
        });
    }

    // =========================================================================
    // 5. DELETE TASK (DELETE: /api/tasks/{taskId})
    // =========================================================================
    // Deletes an existing task with Owner vs Member role-based permissions.
    //
    // AUTHORIZATION POLICY (OWNER VS CREATOR):
    // - Project Owner: Can delete ANY task in the project (administrative privilege).
    // - Project Member: Can ONLY delete tasks that they originally created (task.CreatedById == currentUserId).
    // - Non-Member: 403 Forbidden.
    // =========================================================================
    [HttpDelete("api/tasks/{taskId:guid}")]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        // 1. GET CALLER IDENTITY FROM JWT:
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET TASK:
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound(); // Task does not exist -> 404 Not Found
        }

        // 3. RESOURCE-LEVEL AUTHORIZATION CHECK:
        // Fetch membership record to inspect both project membership AND role (Owner vs Member).
        var membership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == task.ProjectId &&
                x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid(); // Caller does not belong to this project -> 403 Forbidden
        }

        // 4. ROLE-BASED ACCESS CONTROL (RBAC) & CREATOR OWNERSHIP:
        // If the user is NOT an Owner AND is NOT the task creator -> Deny access!
        if (membership.Role != ProjectRole.Owner &&
            task.CreatedById != currentUserId)
        {
            return Forbid(); // Regular member trying to delete another member's task -> 403 Forbidden
        }

        // 5. REMOVE & CASCADE:
        // Deletes the task. Any configured foreign keys (such as comments) will cascade delete if configured in EF Core.
        _dbContext.TaskItems.Remove(task);
        await _dbContext.SaveChangesAsync();

        // 6. RETURN 204 NO CONTENT:
        // HTTP standard response for successful DELETE operations where no response body is needed.
        return NoContent();
    }
}
