namespace TeamTaskManager.Api.Contracts.Projects;

// CONTRACT (DTO):
// Defines what data the outside world (Frontend/Postman) is allowed to send when creating a project.
// Prevents over-posting by only exposing the necessary input properties.
public class CreateProjectRequest
{
    // 'required' keyword ensures C# compiler & model binder enforce a non-empty name.
    public required string Name { get; set; }

    // Nullable string (?) makes description optional.
    public string? Description { get; set; }

    // Explicit WorkspaceId tells the backend which workspace boundary this project belongs to.
    public Guid WorkspaceId { get; set; }
}
