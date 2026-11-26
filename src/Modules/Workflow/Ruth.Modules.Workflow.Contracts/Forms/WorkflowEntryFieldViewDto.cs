namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Evaluated runtime field view for rendering an interactive WorkflowEntry form.</summary>
/// <param name="FieldId">Field definition identifier.</param>
/// <param name="SectionKey">Optional parent section key.</param>
/// <param name="AttributeKey">Bound workflow attribute key.</param>
/// <param name="Label">Display label.</param>
/// <param name="FieldTypeKey">Control field type key.</param>
/// <param name="CurrentValue">Current resolved value.</param>
/// <param name="IsCurrentlyVisible">Evaluated dynamic visibility after applying section + field rules.</param>
/// <param name="IsReadOnly">Whether field is read-only.</param>
/// <param name="IsRequired">Whether field is required when visible.</param>
/// <param name="HelpText">Optional helper caption.</param>
/// <param name="ColumnSpan">12-column span.</param>
/// <param name="SelectOptions">Allowed select choices.</param>
/// <param name="VisibilityRule">Underlying conditional visibility rule for client-side live updates.</param>
public sealed record WorkflowEntryFieldViewDto(
    Guid FieldId,
    string? SectionKey,
    string AttributeKey,
    string Label,
    string FieldTypeKey,
    string? CurrentValue,
    bool IsCurrentlyVisible,
    bool IsReadOnly,
    bool IsRequired,
    string? HelpText,
    int ColumnSpan,
    IReadOnlyList<string> SelectOptions,
    ConditionalVisibilityRuleDto? VisibilityRule
);
