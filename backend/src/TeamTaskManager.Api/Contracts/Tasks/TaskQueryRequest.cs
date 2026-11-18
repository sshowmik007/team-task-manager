using TeamTaskManager.Api.Models;

namespace TeamTaskManager.Api.Contracts.Tasks;

// =============================================================================
// CONTRACT (QUERY DTO): TaskQueryRequest
// =============================================================================
// Encapsulates all query parameters accepted by the GET /api/projects/{projectId}/tasks endpoint.
// In ASP.NET Core, these properties are bound from the URL query string via [FromQuery], e.g.:
// /api/projects/{id}/tasks?search=login&status=1&sortBy=title&page=2&pageSize=10
//
// QUERY CAPABILITIES:
// 1. Full-Text / Keyword Search:
//    - string? Search: Case-sensitive substring filter matching Title or Description.
//
// 2. Status & Assignee Filtering:
//    - TaskItemStatus? Status: Nullable enum (null = all statuses; 0 = Todo, 1 = InProgress, 2 = Done).
//    - Guid? AssigneeId: Nullable GUID (filter tasks assigned to a specific user).
//
// 3. Dynamic Sorting:
//    - string SortBy: Field name to order by ("createdAt", "modifiedAt", "title", "status"). Default: "createdAt".
//    - bool SortDescending: Direction (true = newest/Z-A, false = oldest/A-Z). Default: true.
//
// 4. Safe Server-Side Pagination:
//    - int Page: 1-based page index (defaults to 1).
//    - int PageSize: Number of items per page (defaults to 10; clamped to a maximum of 100 to prevent DoS).
// =============================================================================
public class TaskQueryRequest
{
    // Search keyword applied across Title and Description
    public string? Search { get; set; }

    // Optional status filter (null returns all statuses)
    public TaskItemStatus? Status { get; set; }

    // Optional assignee filter (null returns tasks regardless of assignee)
    public Guid? AssigneeId { get; set; }

    // Column to sort by: "createdAt", "modifiedAt", "title", "status"
    public string SortBy { get; set; } = "createdAt";

    // Sort order: true = descending (newest first), false = ascending (oldest first)
    public bool SortDescending { get; set; } = true;

    // 1-based current page number
    public int Page { get; set; } = 1;

    // Number of records per page (safe default: 10, max: 100)
    public int PageSize { get; set; } = 10;
}
