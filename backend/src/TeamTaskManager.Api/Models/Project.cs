namespace TeamTaskManager.Api.Models
{
    public class Project
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }

        public string? Description { get; set; }

        public Guid WorkspaceId { get; set; }

        public Workspace? Workspace { get; set; }

        public ICollection<ProjectMember> Members { get; set; }
            = new List<ProjectMember>();
    }
}
