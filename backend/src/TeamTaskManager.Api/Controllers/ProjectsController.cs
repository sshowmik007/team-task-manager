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

    // =========================================================================
    // 1. CREATE PROJECT (POST: /api/projects)
    // =========================================================================
    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        // Extract logged-in User ID from cryptographically verified JWT ('sub' claim).
        // Security rule: Never trust the request body for who the user is!
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // Verify parent workspace exists before attaching a project to it
        var workspaceExists = await _dbContext.Workspaces
            .AnyAsync(x => x.Id == request.WorkspaceId);

        if (!workspaceExists)
        {
            return NotFound("Workspace not found.");
        }

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            WorkspaceId = request.WorkspaceId
        };

        _dbContext.Projects.Add(project);

        // AUTOMATIC OWNERSHIP LINK (Relational Security):
        // Automatically links Creator -> Project in ProjectMembers table.
        // Alice logs in -> Alice creates Project A -> Alice = Owner of Project A
        var membership = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectRole.Owner
        };

        _dbContext.ProjectMembers.Add(membership);

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

    // =========================================================================
    // 2. GET PROJECT (GET: /api/projects/{projectId})
    // Resource-level authorization: Any active member (Owner or Member) can view.
    // =========================================================================
    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // MEMBERSHIP CHECK:
        // AnyAsync is sufficient here because we only need true/false (is this user in this project?).
        var isMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId);

        if (!isMember)
        {
            // 403 Forbidden: Caller is authenticated, but not a member of this project.
            return Forbid();
        }

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

    // =========================================================================
    // 3. DELETE PROJECT (DELETE: /api/projects/{projectId})
    // OWNER-ONLY PRIVILEGE: Only the Project Owner can delete the project.
    // =========================================================================
    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        // ROLE-BASED RESOURCE CHECK:
        // Notice we use FirstOrDefaultAsync instead of AnyAsync because we need the
        // actual 'membership.Role' value, not just a true/false existence flag.
        var membership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId);

        // 1. Must belong to the project
        if (membership == null)
        {
            return Forbid();
        }

        // 2. Must be an Owner (Members cannot delete projects!)
        // Bob (Member) calling DELETE -> 403 Forbidden.
        // Alice (Owner) calling DELETE -> Allowed.
        if (membership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project == null)
        {
            return NotFound();
        }

        // EF Core handles cascade delete of child TaskItems, ProjectMembers, and Labels
        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync();

        // 204 No Content indicates successful deletion with an empty response body
        return NoContent();
    }

    // =========================================================================
    // 4. ADD MEMBER TO PROJECT (POST: /api/projects/{projectId}/members)
    // OWNER-ONLY PRIVILEGE: Only the Project Owner can add/invite new members.
    // =========================================================================
    [HttpPost("{projectId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid projectId,
        AddProjectMemberRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 1. Verify caller membership & role: Must be an Owner of this project
        var currentMembership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (currentMembership == null || currentMembership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        // 2. Look up the target user by Email in Identity's Users table
        // AppDbContext inherits IdentityDbContext, exposing _dbContext.Users directly.
        var userToAdd = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (userToAdd == null)
        {
            return NotFound("User not found.");
        }

        // 3. Prevent duplicate memberships: Check if user is already in this project
        var alreadyMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userToAdd.Id);

        if (alreadyMember)
        {
            return BadRequest("User is already a project member.");
        }

        // 4. Create new ProjectMember row with Role = Member
        var membership = new ProjectMember
        {
            ProjectId = projectId,
            UserId = userToAdd.Id,
            Role = ProjectRole.Member
        };

        _dbContext.ProjectMembers.Add(membership);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            membership.ProjectId,
            membership.UserId,
            Role = membership.Role.ToString()
        });
    }

    // =========================================================================
    // 5. LIST PROJECT MEMBERS (GET: /api/projects/{projectId}/members)
    // Accessible by any project collaborator (Owner or Member).
    // =========================================================================
    [HttpGet("{projectId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid projectId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // Verify caller is an active member of this project
        var isMember = await _dbContext.ProjectMembers
            .AnyAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (!isMember)
        {
            return Forbid();
        }

        // EF CORE PROJECTION PATTERN: .Where() -> .Select() -> .ToListAsync()
        // 1. .Where()  filters rows in SQL (WHERE "ProjectId" = @pId)
        // 2. .Select() shapes output into an anonymous DTO, fetching only needed columns
        // 3. .ToListAsync() executes the optimized query asynchronously
        var members = await _dbContext.ProjectMembers
            .Where(x => x.ProjectId == projectId)
            .Select(x => new
            {
                x.UserId,
                x.User!.Email,
                x.User.DisplayName,
                Role = x.Role.ToString()
            })
            .ToListAsync();

        return Ok(members);
    }

    // =========================================================================
    // 6. REMOVE MEMBER FROM PROJECT (DELETE: /api/projects/{projectId}/members/{userId})
    // OWNER-ONLY PRIVILEGE: Only Owner can remove members. Owner cannot be removed.
    // =========================================================================
    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid projectId,
        Guid userId)
    {
        var currentUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(currentUserIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 1. Verify caller is an Owner
        var currentMembership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (currentMembership == null || currentMembership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        // 2. Find target membership record to remove
        var membershipToRemove = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId);

        if (membershipToRemove == null)
        {
            return NotFound("Project member not found.");
        }

        // 3. Business rule: A project cannot remove its Owner (prevents orphaned projects)
        if (membershipToRemove.Role == ProjectRole.Owner)
        {
            return BadRequest("Owner cannot be removed.");
        }

        _dbContext.ProjectMembers.Remove(membershipToRemove);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================================
    // 7. CHANGE MEMBER ROLE (PUT: /api/projects/{projectId}/members/{userId}/role)
    // OWNER-ONLY PRIVILEGE: Only Owner can adjust roles.
    // =========================================================================
    [HttpPut("{projectId:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(
        Guid projectId,
        Guid userId,
        UpdateProjectMemberRoleRequest request)
    {
        // 1. INPUT VALIDATION: Ensure incoming role integer is defined in ProjectRole enum
        if (!Enum.IsDefined(typeof(ProjectRole), request.Role))
        {
            return BadRequest("Invalid project role.");
        }

        // 2. BUSINESS RULE: Maintain single-owner model (do not promote to Owner yet)
        if (request.Role != ProjectRole.Member)
        {
            return BadRequest("Only Member role can be assigned at this stage.");
        }

        var currentUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(currentUserIdValue, out var currentUserId))
        {
            return Unauthorized();
        }

        // 3. Verify caller is an Owner of this project
        var currentMembership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == currentUserId);

        if (currentMembership == null || currentMembership.Role != ProjectRole.Owner)
        {
            return Forbid();
        }

        // 4. Find target member to update
        var membership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(x =>
                x.ProjectId == projectId &&
                x.UserId == userId);

        if (membership == null)
        {
            return NotFound("Project member not found.");
        }

        // 5. BUSINESS RULE: Owner cannot demote or change their own role here
        if (membership.UserId == currentUserId)
        {
            return BadRequest("You cannot change your own owner role.");
        }

        membership.Role = request.Role;
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            membership.UserId,
            Role = membership.Role.ToString()
        });
    }
}
