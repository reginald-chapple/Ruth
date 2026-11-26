namespace Ruth.Modules.Workflow.Contracts.Definitions;

/// <summary>Declarative specification for a typed attribute owned by a WorkflowType or ActivityType.</summary>
/// <param name="Key">Unique attribute key within the workflow scope.</param>
/// <param name="Name">Human-readable display label.</param>
/// <param name="FieldTypeKey">Canonical field type key (e.g., text, memo, email, number, date, boolean, select, currency).</param>
/// <param name="DefaultValue">Optional initial value seeded upon activation.</param>
/// <param name="IsRequired">Whether a non-empty value is required.</param>
/// <param name="Order">Zero-based display sort order.</param>
public sealed record WorkflowAttributeSpec(
    string Key,
    string Name,
    string FieldTypeKey = "text",
    string? DefaultValue = null,
    bool IsRequired = false,
    int Order = 0
);
