using System.Globalization;
using Ruth.Modules.Reporting.Contracts.DataViews;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>
/// Clean-room Query AST compiler translating DataViewFilter trees into parameterized PostgreSQL
/// WHERE predicates over allowlisted columns, jsonb attributes, PostGIS spatial expressions,
/// and cross-module PersonId set intersections.
/// </summary>
public static class DataViewSqlCompiler
{
    public sealed record CompiledSqlPredicate(
        string WhereClause,
        IReadOnlyDictionary<string, object?> Parameters
    );

    public static CompiledSqlPredicate Compile(
        DataViewFilterNodeDto root,
        IReadOnlyDictionary<string, string> allowedColumnMap
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(allowedColumnMap);

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var sql = CompileNode(root, allowedColumnMap, parameters);
        return new CompiledSqlPredicate(string.IsNullOrWhiteSpace(sql) ? "TRUE" : sql, parameters);
    }

    private static string CompileNode(
        DataViewFilterNodeDto node,
        IReadOnlyDictionary<string, string> columns,
        Dictionary<string, object?> parameters
    )
    {
        if (node.ExpressionType == DataViewFilterExpressionType.Rule)
        {
            return CompileRule(node, columns, parameters);
        }

        if (node.Children is not { Count: > 0 })
        {
            return node.ExpressionType is DataViewFilterExpressionType.GroupAny ? "FALSE" : "TRUE";
        }

        List<string> compiledChildren =
        [
            .. node.Children.Select(c => CompileNode(c, columns, parameters)),
        ];

        return node.ExpressionType switch
        {
            DataViewFilterExpressionType.Rule => "TRUE",
            DataViewFilterExpressionType.GroupAll => $"({string.Join(" AND ", compiledChildren)})",
            DataViewFilterExpressionType.GroupAny => $"({string.Join(" OR ", compiledChildren)})",
            DataViewFilterExpressionType.GroupNone =>
                $"NOT ({string.Join(" OR ", compiledChildren)})",
            DataViewFilterExpressionType.GroupAllFalse =>
                $"NOT ({string.Join(" AND ", compiledChildren)})",
            _ => "TRUE",
        };
    }

    private static string CompileRule(
        DataViewFilterNodeDto rule,
        IReadOnlyDictionary<string, string> columns,
        Dictionary<string, object?> parameters
    )
    {
        return rule.RuleKind switch
        {
            DataViewFilterRuleKind.EntityProperty => CompilePropertyRule(rule, columns, parameters),
            DataViewFilterRuleKind.JsonbAttribute => CompileJsonbRule(rule, parameters),
            DataViewFilterRuleKind.SpatialRadius => CompileSpatialRadiusRule(rule, parameters),
            DataViewFilterRuleKind.SpatialPolygon => CompileSpatialPolygonRule(rule, parameters),
            DataViewFilterRuleKind.CrossModulePersonSet => CompileCrossModuleSetRule(
                rule,
                parameters
            ),
            _ => "FALSE",
        };
    }

    private static string CompilePropertyRule(
        DataViewFilterNodeDto rule,
        IReadOnlyDictionary<string, string> columns,
        Dictionary<string, object?> parameters
    )
    {
        return
            string.IsNullOrWhiteSpace(rule.FieldKey)
            || !columns.TryGetValue(rule.FieldKey, out var sqlColumn)
            ? "FALSE"
            : CompileOperator(
                sqlColumn,
                rule.Operator,
                rule.Value,
                rule.SecondaryValue,
                parameters
            );
    }

    private static string CompileJsonbRule(
        DataViewFilterNodeDto rule,
        Dictionary<string, object?> parameters
    )
    {
        var attrParam = AddParam(parameters, rule.FieldKey ?? string.Empty);
        var expr = $"(attribute_values ->> {attrParam})";
        return CompileOperator(expr, rule.Operator, rule.Value, rule.SecondaryValue, parameters);
    }

    private static string CompileSpatialRadiusRule(
        DataViewFilterNodeDto rule,
        Dictionary<string, object?> parameters
    )
    {
        if (
            !DataViewSpatialMath.TryParseLatLon(rule.Value, out var lat, out var lon)
            || !double.TryParse(
                rule.SecondaryValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var meters
            )
        )
        {
            return "FALSE";
        }

        var lonP = AddParam(parameters, lon);
        var latP = AddParam(parameters, lat);
        var distP = AddParam(parameters, meters);
        return $"ST_DWithin(geo_point, ST_SetSRID(ST_MakePoint({lonP}, {latP}), 4326)::geography, {distP})";
    }

    private static string CompileSpatialPolygonRule(
        DataViewFilterNodeDto rule,
        Dictionary<string, object?> parameters
    )
    {
        var wktP = AddParam(parameters, rule.Value ?? string.Empty);
        return $"ST_Covers(ST_GeogFromText({wktP}), geo_point)";
    }

    private static string CompileCrossModuleSetRule(
        DataViewFilterNodeDto rule,
        Dictionary<string, object?> parameters
    )
    {
        var keyP = AddParam(
            parameters,
            DataViewAstEvaluator.BuildPersonSetLookupKey(rule.FieldKey, rule.Value)
        );
        return rule.Operator == DataViewFilterOperator.NotEquals
            ? $"NOT (person_id = ANY(@set_{keyP[1..]}))"
            : $"person_id = ANY(@set_{keyP[1..]})";
    }

    private static string CompileOperator(
        string lhs,
        DataViewFilterOperator op,
        string? val,
        string? secondary,
        Dictionary<string, object?> parameters
    )
    {
        return op switch
        {
            DataViewFilterOperator.IsBlank => $"({lhs} IS NULL OR {lhs}::text = '')",
            DataViewFilterOperator.IsNotBlank => $"({lhs} IS NOT NULL AND {lhs}::text <> '')",
            DataViewFilterOperator.Equals =>
                $"LOWER({lhs}::text) = LOWER({AddParam(parameters, val)})",
            DataViewFilterOperator.NotEquals =>
                $"LOWER(COALESCE({lhs}::text, '')) <> LOWER({AddParam(parameters, val)})",
            DataViewFilterOperator.Contains =>
                $"{lhs}::text ILIKE {AddParam(parameters, $"%{val}%")}",
            DataViewFilterOperator.StartsWith =>
                $"{lhs}::text ILIKE {AddParam(parameters, $"{val}%")}",
            DataViewFilterOperator.EndsWith =>
                $"{lhs}::text ILIKE {AddParam(parameters, $"%{val}")}",
            DataViewFilterOperator.GreaterThan => $"{lhs} > {AddParam(parameters, val)}",
            DataViewFilterOperator.GreaterThanOrEqual => $"{lhs} >= {AddParam(parameters, val)}",
            DataViewFilterOperator.LessThan => $"{lhs} < {AddParam(parameters, val)}",
            DataViewFilterOperator.LessThanOrEqual => $"{lhs} <= {AddParam(parameters, val)}",
            DataViewFilterOperator.In =>
                $"LOWER({lhs}::text) = ANY({AddParam(parameters, SplitCsvLower(val))})",
            DataViewFilterOperator.Between =>
                $"({lhs} >= {AddParam(parameters, val)} AND {lhs} <= {AddParam(parameters, secondary)})",
            _ => "FALSE",
        };
    }

    private static string[] SplitCsvLower(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            :
            [
                .. value
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(static s => s.ToLowerInvariant()),
            ];
    }

    private static string AddParam(Dictionary<string, object?> parameters, object? value)
    {
        var name = string.Create(CultureInfo.InvariantCulture, $"@p{parameters.Count}");
        parameters[name] = value;
        return name;
    }
}
