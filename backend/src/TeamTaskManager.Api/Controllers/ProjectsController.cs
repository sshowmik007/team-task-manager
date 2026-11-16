using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TeamTaskManager.Api.Contracts.Projects;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Route("api/projects")]
// 1. AUTHENTICATION GATE:
// Every request to this controller requires a valid, unexpired JWT.
// If missing or invalid -> ASP.NET Core returns 401 Unauthorized immediately.
// If valid -> Populates HttpContext.User with claims from the token.
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ProjectsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // POST: /api/projects
    // Creates a new project and automatically makes the creator its Owner.
    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        // 2. IDENTITY SOURCE OF TRUTH:
        // Extract the logged-in User ID from the cryptographically verified JWT ('sub' claim).
        // Security rule: Never take the user ID from the request body!
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 3. DEFENSIVE CHECK:
        // Ensure the ID claim in the token is a valid GUID.
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // 4. FOREIGN KEY INTEGRITY CHECK:
        // AnyAsync is lightweight (SELECT 1 ...) to verify the workspace exists
        // before attempting to attach a project to it.
        var workspaceExists = await _dbContext.Workspaces
            .AnyAsync(x => x.Id == request.WorkspaceId);

        if (!workspaceExists)
        {
            return NotFound("Workspace not found.");
        }

        // 5. INSTANTIATE PROJECT:
        // Create the project entity. EF Core will track this and generate project.Id.
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            WorkspaceId = request.WorkspaceId
        };

        _dbContext.Projects.Add(project);

        // 6. AUTOMATIC OWNERSHIP LINK (RELATIONAL SECURITY):
        // Automatically link Creator (User) -> Project in the join table (ProjectMember).
        //
        // Alice logs in
        //     ↓
        // Alice creates Project A
        //     ↓
        // Project A created
        //     ↓
        // ProjectMember created automatically
        //     ↓
        // Alice = Owner of Project A
        var membership = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRole.Owner
        };

        _dbContext.ProjectMembers.Add(membership);

        // 7. ATOMIC TRANSACTION:
        // Commits both Project and ProjectMember together in a single database round-trip.
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            project.Id,
            project.Name,
            project.Description,
            project.WorkspaceId,
            Role = membership.Role.ToString()
        });
    }

    // GET: /api/projects/{projectId}
    // Resource-level authorization: Only members of this project can access it.
    [HttpGet("{projectId:guid}")] // ':guid' route constraint blocks invalid formats early
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        // 1. GET CALLER IDENTITY:
        // Who is making this request?
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(); // Missing or invalid token -> 401
        }

        // 2. RESOURCE-LEVEL AUTHORIZATION CHECK (The Core Defense):
        // Asks the database: "Does a ProjectMember row exist where ProjectId = this project AND UserId = this user?"
        // EF Core runs: SELECT EXISTS(SELECT 1 FROM "ProjectMembers" WHERE "ProjectId" = @pId AND "UserId" = @userId)
        var isMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId);

        // 3. FORBIDDEN CHECK:
        // 401 vs 403 distinction:
        // - 401 Unauthorized = "Who are you?" (Missing or invalid JWT)
        // - 403 Forbidden    = "I know who you are (valid JWT), but you are NOT a member of this project."
        // This completely prevents IDOR (Insecure Direct Object Reference) data leaks between users.
        if (!isMember)
        {
            return Forbid();
        }

        // 4. SAFE RETRIEVAL:
        // Only fetch project details AFTER confirming the caller is an authorized member.
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            project.Id,
            project.Name,
            project.Description,
            project.WorkspaceId
        });
    }
}
