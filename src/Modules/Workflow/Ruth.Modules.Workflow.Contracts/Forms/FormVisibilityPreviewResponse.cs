namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Result of previewing conditional visibility rules in the Form Builder studio.</summary>
/// <param name="VisibleSectionKeys">Section keys currently visible.</param>
/// <param name="VisibleAttributeKeys">Field attribute keys currently visible.</param>
/// <param name="RequiredVisibleAttributeKeys">Currently visible fields that require input.</param>
public sealed record FormVisibilityPreviewResponse(
    IReadOnlyList<string> VisibleSectionKeys,
    IReadOnlyList<string> VisibleAttributeKeys,
    IReadOnlyList<string> RequiredVisibleAttributeKeys
);
