namespace TeamTaskManager.Api.Models;

// DOMAIN MODEL:
// Top-level organizational boundary (tenant/organization) that groups multiple projects.
// Example: Workspace "Acme Software" -> Projects ["Website Redesign", "Mobile App"].
public class Workspace
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    // Navigation property: All projects contained within this workspace
    public List<Project> Projects { get; set; } = new();
}
