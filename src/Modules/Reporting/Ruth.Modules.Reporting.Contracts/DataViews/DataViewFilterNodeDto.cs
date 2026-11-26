namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Declarative node in a DataView Query AST supporting AND/OR/NOT trees and leaf rules.</summary>
/// <param name="ExpressionType">Group combinator or Rule leaf.</param>
/// <param name="RuleKind">Leaf rule kind when ExpressionType is Rule.</param>
/// <param name="FieldKey">Target property name, jsonb attribute key, or cross-module source key.</param>
/// <param name="Operator">Comparison operator for leaf rules.</param>
/// <param name="Value">Primary comparison value (or center lat,lon / referenced DataViewId).</param>
/// <param name="SecondaryValue">Optional secondary value (upper bound for Between, radius meters for SpatialRadius, or exclude flag).</param>
/// <param name="Children">Child AST nodes for group expressions.</param>
public sealed record DataViewFilterNodeDto(
    DataViewFilterExpressionType ExpressionType,
    DataViewFilterRuleKind RuleKind = DataViewFilterRuleKind.EntityProperty,
    string? FieldKey = null,
    DataViewFilterOperator Operator = DataViewFilterOperator.Equals,
    string? Value = null,
    string? SecondaryValue = null,
    IReadOnlyList<DataViewFilterNodeDto>? Children = null
);
