namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Represents a Project container inside a Workspace.
// Projects are the primary resource-level authorization boundary in the application.
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public string? Description { get; set; }

    // Foreign Key: Workspace that owns this project
    public Guid WorkspaceId { get; set; }

    // Navigation property: Reference to parent Workspace entity
    public Workspace? Workspace { get; set; }

    // Navigation property: Team members who have access to this project
    public ICollection<ProjectMember> Members { get; set; }
        = new List<ProjectMember>();

    // Navigation property: Tasks belonging to this project
    public ICollection<TaskItem> Tasks { get; set; }
        = new List<TaskItem>();

    // Navigation property: Custom classification labels created for this project
    public ICollection<Label> Labels { get; set; }
        = new List<Label>();
}
