namespace TeamTaskManager.Api.Models;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public Guid ProjectId { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    public Project? Project { get; set; }

    public ApplicationUser? Assignee { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    public ICollection<Label> Labels { get; set; }
        = new List<Label>();

    public ICollection<Comment> Comments { get; set; }= new List<Comment>();
}