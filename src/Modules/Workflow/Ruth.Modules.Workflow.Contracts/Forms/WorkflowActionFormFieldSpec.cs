namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Specification for an interactive field inside a WorkflowActionForm.</summary>
/// <param name="AttributeKey">Workflow attribute key bound to this field.</param>
/// <param name="Label">Display label.</param>
/// <param name="FieldTypeKey">Field control type (text, memo, email, number, currency, date, boolean, select, rating).</param>
/// <param name="Order">Sort order within its section or form.</param>
/// <param name="SectionKey">Optional parent section key.</param>
/// <param name="IsVisible">Base visibility flag prior to conditional rule evaluation.</param>
/// <param name="IsReadOnly">Renders value as read-only and ignores submitted edits.</param>
/// <param name="IsRequired">Requires a non-empty value when the field is visible.</param>
/// <param name="DefaultValue">Optional default value when workflow attribute is unset.</param>
/// <param name="HelpText">Optional helper caption.</param>
/// <param name="ColumnSpan">Responsive 12-column width (1..12).</param>
/// <param name="SelectOptions">Allowed choices when FieldTypeKey is select.</param>
/// <param name="VisibilityRule">Optional conditional visibility rule.</param>
public sealed record WorkflowActionFormFieldSpec(
    string AttributeKey,
    string Label,
    string FieldTypeKey = "text",
    int Order = 0,
    string? SectionKey = null,
    bool IsVisible = true,
    bool IsReadOnly = false,
    bool IsRequired = false,
    string? DefaultValue = null,
    string? HelpText = null,
    int ColumnSpan = 12,
    IReadOnlyList<string>? SelectOptions = null,
    ConditionalVisibilityRuleDto? VisibilityRule = null
);
