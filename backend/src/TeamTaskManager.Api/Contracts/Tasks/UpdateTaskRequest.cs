using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Contracts.Tasks;

// =============================================================================
// CONTRACT (DTO): UpdateTaskRequest
// =============================================================================
// Defines the exact payload allowed when modifying an existing task.
//
// DESIGN & SECURITY PRINCIPLES:
// 1. Mass Assignment Prevention (Over-Posting Defense):
//    - We ONLY expose mutable properties: Title, Description, Status, and AssigneeId.
//    - Sensitive & Immutable properties are strictly excluded:
//      * Id: Managed by database primary key.
//      * ProjectId: Tasks cannot be moved between projects arbitrarily via an update DTO.
//      * CreatedById: Origin creator identity is permanent audit data.
//      * CreatedAt / ModifiedAt: Managed server-side with UtcNow timestamps.
//
// 2. Data Types:
//    - required string Title: C# 11 required modifier ensures Title cannot be null or omitted.
//    - string? Description: Nullable reference type (can be null or omitted).
//    - TaskItemStatus Status: Strongly typed enum (Todo = 0, InProgress = 1, Done = 2).
//    - Guid? AssigneeId: Nullable GUID - allows assigning to a member OR unassigning (null).
// =============================================================================
public class UpdateTaskRequest
{
    // Required task title (e.g. "Fix navigation bug on mobile")
    public required string Title { get; set; }

    // Optional updated description or markdown notes
    public string? Description { get; set; }

    // Workflow state: Todo (0), InProgress (1), or Done (2)
    public TaskItemStatus Status { get; set; }

    // Nullable Guid: Can assign to a valid project member, or pass null to unassign
    public Guid? AssigneeId { get; set; }
}
