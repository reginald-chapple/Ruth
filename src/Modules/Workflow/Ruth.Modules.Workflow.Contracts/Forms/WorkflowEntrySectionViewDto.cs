namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Evaluated runtime section view for rendering an interactive WorkflowEntry form.</summary>
/// <param name="SectionKey">Section key.</param>
/// <param name="Title">Section heading.</param>
/// <param name="Description">Optional section subheading.</param>
/// <param name="ShowHeadingSeparator">Whether to show divider.</param>
/// <param name="IsCurrentlyVisible">Evaluated dynamic visibility.</param>
/// <param name="Fields">Ordered fields inside this section.</param>
public sealed record WorkflowEntrySectionViewDto(
    string SectionKey,
    string Title,
    string? Description,
    bool ShowHeadingSeparator,
    bool IsCurrentlyVisible,
    IReadOnlyList<WorkflowEntryFieldViewDto> Fields
);
