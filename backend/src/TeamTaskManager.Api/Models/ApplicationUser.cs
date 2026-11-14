using Microsoft.AspNetCore.Identity;

namespace TeamTaskManager.Api.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string ? DisplayName { get; set; }

        public ICollection<ProjectMember> ProjectMemberships { get; set; }
       = new List<ProjectMember>();

    }
}
