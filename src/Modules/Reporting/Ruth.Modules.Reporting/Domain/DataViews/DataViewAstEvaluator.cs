using Ruth.Modules.Reporting.Contracts.DataViews;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Evaluates a DataView Query AST against candidate entity records and resolved cross-module PersonId sets.</summary>
public static class DataViewAstEvaluator
{
    public static bool Evaluate(
        DataViewFilterNodeDto node,
        DataViewEntityRecord record,
        IReadOnlyDictionary<string, IReadOnlySet<Guid>>? crossModulePersonSets = null
    )
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(record);

        return node.ExpressionType switch
        {
            DataViewFilterExpressionType.Rule => EvaluateRule(node, record, crossModulePersonSets),
            DataViewFilterExpressionType.GroupAll => EvaluateAll(
                node.Children,
                record,
                crossModulePersonSets
            ),
            DataViewFilterExpressionType.GroupAny => EvaluateAny(
                node.Children,
                record,
                crossModulePersonSets
            ),
            DataViewFilterExpressionType.GroupNone => !EvaluateAny(
                node.Children,
                record,
                crossModulePersonSets
            ),
            DataViewFilterExpressionType.GroupAllFalse => !EvaluateAll(
                node.Children,
                record,
                crossModulePersonSets
            ),
            _ => false,
        };
    }

    private static bool EvaluateAll(
        IReadOnlyList<DataViewFilterNodeDto>? children,
        DataViewEntityRecord record,
        IReadOnlyDictionary<string, IReadOnlySet<Guid>>? sets
    )
    {
        if (children is not { Count: > 0 })
        {
            return true;
        }

        foreach (var child in children)
        {
            if (!Evaluate(child, record, sets))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateAny(
        IReadOnlyList<DataViewFilterNodeDto>? children,
        DataViewEntityRecord record,
        IReadOnlyDictionary<string, IReadOnlySet<Guid>>? sets
    )
    {
        if (children is not { Count: > 0 })
        {
            return false;
        }

        foreach (var child in children)
        {
            if (Evaluate(child, record, sets))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EvaluateRule(
        DataViewFilterNodeDto rule,
        DataViewEntityRecord record,
        IReadOnlyDictionary<string, IReadOnlySet<Guid>>? sets
    )
    {
        return rule.RuleKind switch
        {
            DataViewFilterRuleKind.EntityProperty => EvaluatePropertyRule(rule, record),
            DataViewFilterRuleKind.JsonbAttribute => EvaluateAttributeRule(rule, record),
            DataViewFilterRuleKind.SpatialRadius => DataViewSpatialMath.IsWithinRadius(
                record.Latitude,
                record.Longitude,
                rule.Value,
                rule.SecondaryValue
            ),
            DataViewFilterRuleKind.SpatialPolygon => DataViewSpatialMath.IsInsidePolygon(
                record.Latitude,
                record.Longitude,
                rule.Value
            ),
            DataViewFilterRuleKind.CrossModulePersonSet => EvaluatePersonSetRule(
                rule,
                record,
                sets
            ),
            _ => false,
        };
    }

    private static bool EvaluatePropertyRule(
        DataViewFilterNodeDto rule,
        DataViewEntityRecord record
    )
    {
        object? actual = null;
        if (!string.IsNullOrWhiteSpace(rule.FieldKey))
        {
            _ = record.Values.TryGetValue(rule.FieldKey, out actual);
        }

        return DataViewScalarComparer.Evaluate(
            actual,
            rule.Operator,
            rule.Value,
            rule.SecondaryValue
        );
    }

    private static bool EvaluateAttributeRule(
        DataViewFilterNodeDto rule,
        DataViewEntityRecord record
    )
    {
        string? actual = null;
        if (record.Attributes is not null && !string.IsNullOrWhiteSpace(rule.FieldKey))
        {
            _ = record.Attributes.TryGetValue(rule.FieldKey, out actual);
        }

        return DataViewScalarComparer.Evaluate(
            actual,
            rule.Operator,
            rule.Value,
            rule.SecondaryValue
        );
    }

    private static bool EvaluatePersonSetRule(
        DataViewFilterNodeDto rule,
        DataViewEntityRecord record,
        IReadOnlyDictionary<string, IReadOnlySet<Guid>>? sets
    )
    {
        var key = BuildPersonSetLookupKey(rule.FieldKey, rule.Value);
        var personId = record.PersonId ?? record.EntityId;
        var inSet =
            sets is not null && sets.TryGetValue(key, out var ids) && ids.Contains(personId);
        return rule.Operator == DataViewFilterOperator.NotEquals ? !inSet : inSet;
    }

    public static string BuildPersonSetLookupKey(string? sourceKind, string? targetValue)
    {
        var kind = string.IsNullOrWhiteSpace(sourceKind) ? "dataview" : sourceKind.Trim();
        var value = targetValue?.Trim() ?? string.Empty;
        return $"{kind}:{value}".ToLowerInvariant();
    }
}
