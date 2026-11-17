namespace TeamTaskManager.Api.Contracts.Tasks;

// CONTRACT (DTO):
// Defines what the client sends when creating a new task inside a project.
// Security Rule: Notice there is NO 'CreatedById' or 'Status' here!
// - CreatedById is extracted safely from the authenticated JWT token.
// - Status defaults automatically to TaskItemStatus.Todo.
public class CreateTaskRequest
{
    // 'required' ensures the task title is provided by the client
    public required string Title { get; set; }

    // Optional detailed notes or checklist for the task
    public string? Description { get; set; }

    // Optional Guid: If null, task is created unassigned.
    // If provided, the backend verifies this user is an active member of this project.
    public Guid? AssigneeId { get; set; }
}
