namespace TeamTaskManager.Api.Models;

// ENUM:
// Defines user permission tiers within an individual project.
// Stored as an integer in the database (1 = Owner, 2 = Member).
public enum ProjectRole
{
    // Full project control: can edit project settings, manage members, and delete project
    Owner = 1,

    // Standard collaborator: can create tasks, assign work, update status, and post comments
    Member = 2
}
