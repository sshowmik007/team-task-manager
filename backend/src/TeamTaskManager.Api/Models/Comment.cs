namespace TeamTaskManager.Api.Models;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskItemId { get; set; }

    public Guid AuthorId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.UtcNow;

    public TaskItem? TaskItem { get; set; }

    public ApplicationUser? Author { get; set; }
}