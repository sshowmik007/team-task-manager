namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Reusable tag used to categorize and filter tasks within a project (e.g. 'bug', 'frontend', 'urgent').
// Scoped to a specific project to avoid cross-project namespace pollution.
public class Label
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key: Project that defines this label
    public Guid ProjectId { get; set; }

    public required string Name { get; set; }

    // Navigation properties
    public Project? Project { get; set; }

    // Many-to-Many: Multiple tasks can have this label, and a task can have multiple labels
    public ICollection<TaskItem> Tasks { get; set; }
        = new List<TaskItem>();
}
