namespace TeamTaskManager.Api.Models;

// ENUM:
// Represents the linear workflow lifecycle states of a TaskItem on the Kanban board.
public enum TaskItemStatus
{
    // 1. Initial backlog/planned state
    Todo = 1,

    // 2. Active work underway by assignee
    InProgress = 2,

    // 3. Implementation complete, ready for peer or QA review
    Review = 3,

    // 4. Verification complete, task closed
    Done = 4
}
