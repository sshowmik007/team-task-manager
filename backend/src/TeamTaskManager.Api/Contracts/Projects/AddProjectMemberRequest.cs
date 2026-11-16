namespace TeamTaskManager.Api.Contracts.Projects;

// CONTRACT (DTO):
// Defines input required to add a team member to a project.
// The Owner provides the target user's registered email address.
public class AddProjectMemberRequest
{
    public required string Email { get; set; }
}
