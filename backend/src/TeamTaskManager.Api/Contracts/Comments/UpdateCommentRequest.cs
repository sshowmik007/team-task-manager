namespace TeamTaskManager.Api.Contracts.Comments;

// =============================================================================
// CONTRACT (DTO): UpdateCommentRequest
// =============================================================================
// Defines the payload allowed when editing an existing comment.
//
// DESIGN & SECURITY PRINCIPLES:
// 1. Minimal Mutable Surface:
//    - Only 'Body' is editable.
//    - Immutable audit properties (Id, TaskItemId, AuthorId, CreatedAt) cannot be
//      overridden by the client.
//    - ModifiedAt is stamped server-side on every successful update.
//
// 2. Data Types:
//    - required string Body: Required updated text of the comment.
// =============================================================================
public class UpdateCommentRequest
{
    // The updated text content of the comment
    public required string Body { get; set; }
}
