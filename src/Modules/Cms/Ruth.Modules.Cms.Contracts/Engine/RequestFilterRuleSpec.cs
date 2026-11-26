namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Declarative rule evaluated against an incoming web or portal request.</summary>
public sealed record RequestFilterRuleSpec(
    RequestFilterRuleKind Kind,
    RequestFilterComparison Comparison,
    string Key,
    string? Value = null
);
