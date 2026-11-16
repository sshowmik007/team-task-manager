namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Central work item entity representing a task inside a project.
// Named 'TaskItem' rather than 'Task' to prevent collision with System.Threading.Tasks.Task.
public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Title { get; set; }

    public string? Description { get; set; }

    // Workflow state: Todo -> InProgress -> Review -> Done
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    // Foreign Key: Project this task belongs to (Cascade delete on project deletion)
    public Guid ProjectId { get; set; }

    // Foreign Key (Nullable): Assigned team member (DeleteBehavior.Restrict protects user deletion)
    public Guid? AssigneeId { get; set; }

    // Foreign Key: User who created this task (DeleteBehavior.Restrict)
    public Guid CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Project? Project { get; set; }

    public ApplicationUser? Assignee { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    // Many-to-Many relationship with Label entity
    public ICollection<Label> Labels { get; set; }
        = new List<Label>();

    // One-to-Many relationship with Comment entity
    public ICollection<Comment> Comments { get; set; }
        = new List<Comment>();
}
