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
    // 2. LIST TASKS WITH SEARCH, FILTERING, SORTING & PAGINATION
    //    (GET: /api/projects/{projectId}/tasks)
    // =========================================================================
    // Demonstrates advanced EF Core Deferred Execution:
    // - Builds an IQueryable pipeline dynamically step-by-step.
    // - SQL is only compiled and executed when terminal operators (CountAsync, ToListAsync) are called.
    // - Bounds clamping protects against DoS (e.g. requesting 1,000,000 rows at once).
    // =========================================================================
    [HttpGet("api/projects/{projectId:guid}/tasks")]
    public async Task<IActionResult> GetTasks(
        Guid projectId,
        [FromQuery] TaskQueryRequest request)
    {
        // 1. EXTRACT CALLER IDENTITY FROM JWT:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. PROJECT ACCESS CHECK:
        // Caller must be an active project member to query tasks.
        var isProjectMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == projectId &&
                    x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        // 3. PAGINATION BOUNDS SANITIZATION:
        // Prevents zero or negative page offsets, and caps PageSize to 100 to avoid memory exhaustion.
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }

        // 4. BASE QUERY (DEFERRED EXECUTION):
        // AsQueryable() allows us to append WHERE and ORDER BY clauses before querying the database.
        var query =
            _dbContext.TaskItems
                .Where(x => x.ProjectId == projectId)
                .AsQueryable();

        // 5. SEARCH FILTER (KEYWORD IN TITLE OR DESCRIPTION):
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Title.Contains(search) ||
                (x.Description != null &&
                 x.Description.Contains(search)));
        }

        // 6. STATUS FILTER (NULLABLE ENUM):
        if (request.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == request.Status.Value);
        }

        // 7. ASSIGNEE FILTER:
        if (request.AssigneeId.HasValue)
        {
            query = query.Where(x =>
                x.AssigneeId == request.AssigneeId.Value);
        }

        // 8. DYNAMIC SORTING (PATTERN MATCHING SWITCH):
        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending
                ? query.OrderByDescending(x => x.Title)
                : query.OrderBy(x => x.Title),

            "status" => request.SortDescending
                ? query.OrderByDescending(x => x.Status)
                : query.OrderBy(x => x.Status),

            "modifiedat" => request.SortDescending
                ? query.OrderByDescending(x => x.ModifiedAt)
                : query.OrderBy(x => x.ModifiedAt),

            _ => request.SortDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt)
        };

        // 9. TOTAL COUNT (FOR PAGINATION METADATA):
        // Executes: SELECT COUNT(*) FROM "TaskItems" WHERE ...
        var totalCount =
            await query.CountAsync();

        // 10. PAGINATION (.Skip & .Take) AND SAFE PROJECTION:
        // .Skip((page - 1) * pageSize): Skips previous pages.
        // .Take(pageSize): Takes only this page's items.
        // .Select(...): Shapes the exact JSON response DTO and loads attached Labels & Assignee safely.
        var tasks =
            await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Description,
                    x.Status,
                    x.ProjectId,
                    x.AssigneeId,

                    Assignee = x.Assignee == null
                        ? null
                        : new
                        {
                            x.Assignee.Id,
                            x.Assignee.DisplayName,
                            x.Assignee.Email
                        },

                    Labels = x.Labels.Select(label => new
                    {
                        label.Id,
                        label.Name
                    }),

                    x.CreatedById,
                    x.CreatedAt,
                    x.ModifiedAt
                })
                .ToListAsync();

        // 11. ENVELOPE RESPONSE WITH PAGINATION METADATA:
        return Ok(new
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,

            TotalPages = (int)Math.Ceiling(
                totalCount / (double)request.PageSize),

            Items = tasks
        });
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
