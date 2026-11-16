using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Contracts.Projects;

// CONTRACT (DTO):
// Defines input required to update a member's role within a project.
// Typed to ProjectRole enum (Owner = 1, Member = 2).
public class UpdateProjectMemberRoleRequest
{
    public ProjectRole Role { get; set; }
}
