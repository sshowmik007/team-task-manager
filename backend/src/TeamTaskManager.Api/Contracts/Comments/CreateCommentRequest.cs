namespace TeamTaskManager.Api.Contracts.Comments;

// =============================================================================
// CONTRACT (DTO): CreateCommentRequest
// =============================================================================
// Defines the payload allowed when posting a comment on a task.
//
// DESIGN & SECURITY PRINCIPLES:
// 1. Mass Assignment (Over-Posting) Prevention:
//    - Notice we do NOT accept 'AuthorId', 'TaskItemId', or 'CreatedAt' from the client JSON!
//      * AuthorId is extracted securely from the cryptographically verified JWT claims.
//      * TaskItemId is bound directly from the URL route parameter (/api/tasks/{taskId}/comments).
//      * CreatedAt is stamped server-side using DateTimeOffset.UtcNow.
//    - This ensures users cannot impersonate other commenters or back-date comments.
//
// 2. Data Types:
//    - required string Body: C# 11 required modifier ensures the comment content
//      cannot be null or omitted during deserialization.
// =============================================================================
public class CreateCommentRequest
{
    // The text content of the comment (markdown or plain text)
    public required string Body { get; set; }
}
