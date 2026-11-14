using Microsoft.AspNetCore.Identity;

namespace TeamTaskManager.Api.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string ? DisplayName { get; set; }

        public ICollection<ProjectMember> ProjectMemberships { get; set; }
       = new List<ProjectMember>();

        public ICollection<TaskItem> AssignedTasks { get; set; }
      = new List<TaskItem>();

        public ICollection<TaskItem> CreatedTasks { get; set; }
         = new List<TaskItem>();

        public ICollection<Comment> Comments { get; set; }
        = new List<Comment>();

    }
}
