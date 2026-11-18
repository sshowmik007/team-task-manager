namespace TeamTaskManager.Api.Contracts.Labels;

// =============================================================================
// CONTRACT (DTO): CreateLabelRequest
// =============================================================================
// Defines the payload allowed when creating a new categorization tag inside a project.
//
// DESIGN & SECURITY PRINCIPLES:
// 1. Scoped Project Association:
//    - Notice 'ProjectId' is omitted from the request body!
//    - The project ID is bound directly from the URL route (/api/projects/{projectId}/labels).
//    - This ensures labels cannot be forged or injected across project boundaries.
//
// 2. Data Types:
//    - required string Name: C# 11 required modifier ensures label names
//      cannot be null or omitted during deserialization (e.g. "Bug", "Frontend").
// =============================================================================
public class CreateLabelRequest
{
    // The descriptive name of the label (e.g., "Bug", "Enhancement", "High Priority")
    public required string Name { get; set; }
}
