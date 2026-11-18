using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamTaskManager.Api.Contracts.Labels;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
// 1. AUTHENTICATION GATE:
// Every endpoint in this controller requires a valid, unexpired JWT bearer token.
// Missing or invalid token -> 401 Unauthorized immediately.
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public LabelsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // =========================================================================
    // 1. CREATE LABEL (POST: /api/projects/{projectId}/labels)
    // =========================================================================
    // Creates a new categorization label scoped to a specific project.
    //
    // AUTHORIZATION POLICY (OWNER ONLY):
    // - Only Project Owners can define new project taxonomy/labels.
    // - Regular Members: 403 Forbidden.
    //
    // UNIQUENESS VALIDATION:
    // - A project cannot have two labels with the exact same name (case-sensitive).
    // - Friendly application check returns 400 Bad Request before database unique index throws.
    // =========================================================================
    [HttpPost("api/projects/{projectId:guid}/labels")]
    public async Task<IActionResult> CreateLabel(
        Guid projectId,
        CreateLabelRequest request)
    {
        // 1. EXTRACT CALLER IDENTITY FROM JWT:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. RESOURCE-LEVEL ROLE CHECK (OWNER ONLY):
        var membership =
            await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == projectId &&
                    x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid(); // Non-member -> 403 Forbidden
        }

        if (membership.Role != ProjectRole.Owner)
        {
            return Forbid(); // Regular member -> 403 Forbidden (Only Owner can create labels)
        }

        // 3. DUPLICATE CHECK WITHIN PROJECT:
        // Validates that (ProjectId + Name) is unique within this project.
        var duplicateExists =
            await _dbContext.Labels
                .AnyAsync(x =>
                    x.ProjectId == projectId &&
                    x.Name == request.Name);

        if (duplicateExists)
        {
            return BadRequest("Label already exists.");
        }

        // 4. INSTANTIATE AND PERSIST LABEL:
        var label = new Label
        {
            ProjectId = projectId,
            Name = request.Name
        };

        _dbContext.Labels.Add(label);

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            label.Id,
            label.Name,
            label.ProjectId
        });
    }

    // =========================================================================
    // 2. READ PROJECT LABELS (GET: /api/projects/{projectId}/labels)
    // =========================================================================
    // Retrieves all available labels defined inside a specific project.
    //
    // AUTHORIZATION POLICY (ALL PROJECT MEMBERS):
    // - Any project member (Owner or Member) can view project labels to tag tasks.
    // - Alphabetical sorting: .OrderBy(x => x.Name) provides consistent UI dropdown order.
    // =========================================================================
    [HttpGet("api/projects/{projectId:guid}/labels")]
    public async Task<IActionResult> GetLabels(Guid projectId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. PROJECT ACCESS CHECK:
        var isMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == projectId &&
                    x.UserId == currentUserId);

        if (!isMember)
        {
            return Forbid(); // Caller does not belong to project -> 403 Forbidden
        }

        // 3. EF CORE PROJECTION PIPELINE:
        var labels =
            await _dbContext.Labels
                .Where(x => x.ProjectId == projectId)
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.ProjectId
                })
                .ToListAsync();

        return Ok(labels);
    }

    // =========================================================================
    // 3. UPDATE LABEL (PUT: /api/labels/{labelId})
    // =========================================================================
    // Renames an existing label.
    //
    // AUTHORIZATION POLICY (OWNER ONLY):
    // - Only Project Owners can rename labels.
    //
    // SELF-EXCLUDING UNIQUENESS CHECK:
    // - x.Id != labelId ensures the label does not detect itself as a duplicate if the name is unchanged.
    // =========================================================================
    [HttpPut("api/labels/{labelId:guid}")]
    public async Task<IActionResult> UpdateLabel(
        Guid labelId,
        UpdateLabelRequest request)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET LABEL:
        var label =
            await _dbContext.Labels
                .FirstOrDefaultAsync(x => x.Id == labelId);

        if (label == null)
        {
            return NotFound("Label not found.");
        }

        // 3. RESOURCE-LEVEL ROLE CHECK (OWNER ONLY):
        var membership =
            await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == label.ProjectId &&
                    x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid();
        }

        if (membership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        // 4. DUPLICATE CHECK (EXCLUDING CURRENT LABEL):
        // Prevents collision with another label in the same project while ignoring self.
        var duplicateExists =
            await _dbContext.Labels
                .AnyAsync(x =>
                    x.ProjectId == label.ProjectId &&
                    x.Name == request.Name &&
                    x.Id != labelId);

        if (duplicateExists)
        {
            return BadRequest("Another label with this name already exists.");
        }

        // 5. APPLY UPDATE & PERSIST:
        label.Name = request.Name;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            label.Id,
            label.Name,
            label.ProjectId
        });
    }

    // =========================================================================
    // 4. DELETE LABEL (DELETE: /api/labels/{labelId})
    // =========================================================================
    // Deletes an existing label from the project.
    //
    // AUTHORIZATION POLICY (OWNER ONLY):
    // - Only Project Owners can delete labels.
    // - EF Core automatically clears associations in the junction table between this label and tasks.
    // =========================================================================
    [HttpDelete("api/labels/{labelId:guid}")]
    public async Task<IActionResult> DeleteLabel(Guid labelId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TARGET LABEL:
        var label =
            await _dbContext.Labels
                .FirstOrDefaultAsync(x => x.Id == labelId);

        if (label == null)
        {
            return NotFound("Label not found.");
        }

        // 3. RESOURCE-LEVEL ROLE CHECK (OWNER ONLY):
        var membership =
            await _dbContext.ProjectMembers
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == label.ProjectId &&
                    x.UserId == currentUserId);

        if (membership == null)
        {
            return Forbid();
        }

        if (membership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        // 4. REMOVE & PERSIST:
        _dbContext.Labels.Remove(label);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================================
    // 5. ATTACH LABEL TO TASK (POST: /api/tasks/{taskId}/labels/{labelId})
    // =========================================================================
    // Associates a project label with a task via the Many-to-Many junction relationship.
    //
    // SECURITY & BOUNDARY RULES:
    // - Project Member: Any project member (Owner or Member) can tag tasks.
    // - Cross-Project Boundary Defense:
    //   label.ProjectId MUST equal task.ProjectId!
    //   Prevents injecting a label from Project B onto a task in Project A.
    // - Idempotency: Returns 400 Bad Request if already attached to avoid duplicates.
    // =========================================================================
    [HttpPost("api/tasks/{taskId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> AddLabelToTask(
        Guid taskId,
        Guid labelId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TASK WITH LABELS COLLECTION:
        // .Include(x => x.Labels) eager-loads existing labels for duplicate checking and collection mutation.
        var task =
            await _dbContext.TaskItems
                .Include(x => x.Labels)
                .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        // 3. VERIFY CALLER BELONGS TO TASK'S PROJECT:
        var isMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == task.ProjectId &&
                    x.UserId == currentUserId);

        if (!isMember)
        {
            return Forbid();
        }

        // 4. LOOK UP LABEL:
        var label =
            await _dbContext.Labels
                .FirstOrDefaultAsync(x => x.Id == labelId);

        if (label == null)
        {
            return NotFound("Label not found.");
        }

        // 5. CROSS-PROJECT BOUNDARY INTEGRITY CHECK:
        // Ensures both the task and the label live in the exact same project!
        if (label.ProjectId != task.ProjectId)
        {
            return BadRequest(
                "Label and task must belong to the same project.");
        }

        // 6. DUPLICATE ATTACHMENT CHECK:
        var alreadyAttached =
            task.Labels.Any(x => x.Id == labelId);

        if (alreadyAttached)
        {
            return BadRequest("Label is already attached to this task.");
        }

        // 7. ATTACH & PERSIST:
        // EF Core adds a new row to the hidden/managed junction table (e.g. LabelTaskItem).
        task.Labels.Add(label);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================================
    // 6. REMOVE LABEL FROM TASK (DELETE: /api/tasks/{taskId}/labels/{labelId})
    // =========================================================================
    // Detaches a label from a task.
    //
    // PERMISSION RULE:
    // - Any project member can untag a label from a task.
    // =========================================================================
    [HttpDelete("api/tasks/{taskId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> RemoveLabelFromTask(
        Guid taskId,
        Guid labelId)
    {
        // 1. EXTRACT CALLER IDENTITY:
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 2. LOOK UP TASK WITH LABELS:
        var task =
            await _dbContext.TaskItems
                .Include(x => x.Labels)
                .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        // 3. VERIFY PROJECT MEMBERSHIP:
        var isMember =
            await _dbContext.ProjectMembers
                .AnyAsync(x =>
                    x.ProjectId == task.ProjectId &&
                    x.UserId == currentUserId);

        if (!isMember)
        {
            return Forbid();
        }

        // 4. LOCATE ATTACHED LABEL IN THE TASK'S LABELS COLLECTION:
        var label =
            task.Labels
                .FirstOrDefault(x => x.Id == labelId);

        if (label == null)
        {
            return NotFound("Label is not attached to this task.");
        }

        // 5. DETACH & PERSIST:
        // EF Core deletes the junction row connecting this task and label without deleting the label itself.
        task.Labels.Remove(label);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
