namespace TeamTaskManager.Api.Models;

public class Label
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public required string Name { get; set; }

    public Project? Project { get; set; }

    public ICollection<TaskItem> Tasks { get; set; }
        = new List<TaskItem>();
}