namespace Ruth.Modules.Workflow.Contracts.Forms;

/// <summary>Single conditional predicate comparing a workflow/form attribute value.</summary>
/// <param name="SourceAttributeKey">Attribute key whose live value is inspected.</param>
/// <param name="Operator">Comparison operator.</param>
/// <param name="ComparisonValue">Expected comparison value when applicable.</param>
public sealed record FieldVisibilityConditionDto(
    string SourceAttributeKey,
    FieldVisibilityOperator Operator,
    string? ComparisonValue = null
);
