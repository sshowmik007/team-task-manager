using Microsoft.AspNetCore.Identity;

namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Represents an authenticated user in the system.
// Inherits from IdentityUser<Guid>, which provides built-in fields:
// - Id (Guid primary key)
// - UserName, NormalizedUserName
// - Email, NormalizedEmail
// - PasswordHash (PBKDF2 salted hash)
// - SecurityStamp, ConcurrencyStamp
public class ApplicationUser : IdentityUser<Guid>
{
    // Application-specific custom profile property
    public string? DisplayName { get; set; }

    // Navigation property: Projects this user belongs to (via ProjectMember join table)
    public ICollection<ProjectMember> ProjectMemberships { get; set; }
        = new List<ProjectMember>();

    // Navigation property: Tasks assigned to this user
    public ICollection<TaskItem> AssignedTasks { get; set; }
        = new List<TaskItem>();

    // Navigation property: Tasks created by this user
    public ICollection<TaskItem> CreatedTasks { get; set; }
        = new List<TaskItem>();

    // Navigation property: Comments authored by this user
    public ICollection<Comment> Comments { get; set; }
        = new List<Comment>();
}
