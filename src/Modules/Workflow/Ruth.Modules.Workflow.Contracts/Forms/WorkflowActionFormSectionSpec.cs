namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Specification for a visual grouping section in a WorkflowActionForm.</summary>
/// <param name="SectionKey">Unique section key within the form.</param>
/// <param name="Title">Section heading.</param>
/// <param name="Order">Display order.</param>
/// <param name="Description">Optional section subheading.</param>
/// <param name="ShowHeadingSeparator">Whether to render a visual divider below the heading.</param>
/// <param name="VisibilityRule">Optional conditional visibility rule governing the entire section.</param>
public sealed record WorkflowActionFormSectionSpec(
    string SectionKey,
    string Title,
    int Order = 0,
    string? Description = null,
    bool ShowHeadingSeparator = true,
    ConditionalVisibilityRuleDto? VisibilityRule = null
);
