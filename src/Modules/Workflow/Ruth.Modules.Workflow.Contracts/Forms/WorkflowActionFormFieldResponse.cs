namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Read projection of a configured WorkflowActionFormField.</summary>
/// <param name="Id">Field identifier.</param>
/// <param name="SectionKey">Optional parent section key.</param>
/// <param name="AttributeKey">Bound workflow attribute key.</param>
/// <param name="Label">Display label.</param>
/// <param name="FieldTypeKey">Control field type key.</param>
/// <param name="Order">Sort order.</param>
/// <param name="IsVisible">Base visibility flag.</param>
/// <param name="IsReadOnly">Read-only flag.</param>
/// <param name="IsRequired">Required flag when visible.</param>
/// <param name="DefaultValue">Optional default value.</param>
/// <param name="HelpText">Optional helper caption.</param>
/// <param name="ColumnSpan">12-grid column span.</param>
/// <param name="SelectOptions">Allowed select choices.</param>
/// <param name="VisibilityRule">Optional conditional visibility rule.</param>
public sealed record WorkflowActionFormFieldResponse(
    Guid Id,
    string? SectionKey,
    string AttributeKey,
    string Label,
    string FieldTypeKey,
    int Order,
    bool IsVisible,
    bool IsReadOnly,
    bool IsRequired,
    string? DefaultValue,
    string? HelpText,
    int ColumnSpan,
    IReadOnlyList<string> SelectOptions,
    ConditionalVisibilityRuleDto? VisibilityRule
);
