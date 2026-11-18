namespace TeamTaskManager.Api.Contracts.Labels;

// =============================================================================
// CONTRACT (DTO): UpdateLabelRequest
// =============================================================================
// Defines the payload allowed when renaming an existing label.
//
// DESIGN & SECURITY PRINCIPLES:
// 1. Scoped Rename:
//    - Only 'Name' can be updated.
//    - Immutable properties (Id, ProjectId) cannot be altered via this request.
//
// 2. Data Types:
//    - required string Name: The new name for the label.
// =============================================================================
public class UpdateLabelRequest
{
    // The new descriptive name for the label
    public required string Name { get; set; }
}
