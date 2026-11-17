namespace TeamTaskManager.Api.Contracts.Tasks
{
    public class CreateTaskRequest
    {
        public required string Title { get; set; }
        public string? Description { get; set; }

        public Guid? AssigneeId { get; set; }
    }
}
