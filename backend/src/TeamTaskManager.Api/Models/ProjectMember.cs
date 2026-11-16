namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL / JOIN ENTITY:
// Connects User <-> Project with a specific Role (Owner, Member).
// This table is the core of Resource-Level Authorization:
// Querying this table answers: "Is User X allowed to access Project Y, and with what permission?"
public class ProjectMember
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key: Target project
    public Guid ProjectId { get; set; }

    // Foreign Key: Member user
    public Guid UserId { get; set; }

    // Role within this specific project (ProjectRole.Owner or ProjectRole.Member)
    public ProjectRole Role { get; set; }

    // Audit timestamp when the user joined the project
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties for EF Core relationship mapping
    public Project? Project { get; set; }

    public ApplicationUser? User { get; set; }
}
