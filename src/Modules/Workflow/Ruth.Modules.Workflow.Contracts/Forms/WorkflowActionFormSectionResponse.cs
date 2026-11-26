namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Read projection of a WorkflowActionFormSection.</summary>
/// <param name="Id">Section identifier.</param>
/// <param name="SectionKey">Unique section key within the form.</param>
/// <param name="Title">Section heading.</param>
/// <param name="Description">Optional section subheading.</param>
/// <param name="Order">Sort order.</param>
/// <param name="ShowHeadingSeparator">Whether to render a divider.</param>
/// <param name="VisibilityRule">Optional section-level conditional visibility rule.</param>
public sealed record WorkflowActionFormSectionResponse(
    Guid Id,
    string SectionKey,
    string Title,
    string? Description,
    int Order,
    bool ShowHeadingSeparator,
    ConditionalVisibilityRuleDto? VisibilityRule
);
