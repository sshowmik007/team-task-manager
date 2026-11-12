
namespace TeamTaskManager.Api.Models
{
    public class Workspace
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }

        public List<Project> Projects { get; set; } = new();
    }
}