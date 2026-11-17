using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using TeamTaskManager.Api.Contracts.Tasks;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;
    [ApiController]
    [Authorize]

    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
    public TasksController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("api/projects/{projectId:guid}/tasks")]
    public async Task<IActionResult> CreateTask(
        Guid projectId,
        CreateTaskRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var isProjectMember =
           await _dbContext.ProjectMembers
               .AnyAsync(x =>
                   x.ProjectId == projectId &&
                   x.UserId == currentUserId);

        if (!isProjectMember)
        {
            return Forbid();
        }

        if (request.AssigneeId.HasValue)
        {
            var assigneeIsMember =
                await _dbContext.ProjectMembers
                    .AnyAsync(x =>
                        x.ProjectId == projectId &&
                        x.UserId == request.AssigneeId.Value);

            if (!assigneeIsMember)
            {
                return BadRequest(
                    "Assignee must be a member of the project.");
            }
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            ProjectId = projectId,
            AssigneeId = request.AssigneeId,
            CreatedById = currentUserId,
            Status = TaskItemStatus.Todo
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

}

