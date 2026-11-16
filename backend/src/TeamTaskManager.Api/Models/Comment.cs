namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Represents a collaborative discussion message posted on a specific task.
// Security: Access to comments is strictly bound to authorization on the parent TaskItem & Project.
public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key: Task this comment belongs to (Cascade delete if task is deleted)
    public Guid TaskItemId { get; set; }

    // Foreign Key: User who wrote the comment (Restrict delete)
    public Guid AuthorId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public TaskItem? TaskItem { get; set; }

    public ApplicationUser? Author { get; set; }
}
