using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Domain.DataViews;

/// <summary>Validates DataView Query AST structure, nesting depth, and leaf rule invariants.</summary>
public static class DataViewAstValidator
{
    public const int MaxDepth = 8;
    public const int MaxNodeCount = 128;

    public static Result Validate(DataViewFilterNodeDto? root)
    {
        if (root is null)
        {
            return Result.Failure(
                ReportingErrors.Validation(
                    "DataView.RootFilterRequired",
                    "Root filter is required."
                )
            );
        }

        var count = 0;
        return ValidateNode(root, depth: 0, ref count);
    }

    private static Result ValidateNode(DataViewFilterNodeDto node, int depth, ref int count)
    {
        count++;
        if (depth > MaxDepth)
        {
            return Result.Failure(
                ReportingErrors.Validation(
                    "DataView.FilterDepthExceeded",
                    $"DataView filter nesting cannot exceed {MaxDepth} levels."
                )
            );
        }

        if (count > MaxNodeCount)
        {
            return Result.Failure(
                ReportingErrors.Validation(
                    "DataView.FilterNodeLimitExceeded",
                    $"DataView filter cannot exceed {MaxNodeCount} nodes."
                )
            );
        }

        if (node.ExpressionType == DataViewFilterExpressionType.Rule)
        {
            return ValidateRule(node);
        }

        if (node.Children is null)
        {
            return Result.Success();
        }

        foreach (var child in node.Children)
        {
            var res = ValidateNode(child, depth + 1, ref count);
            if (res.IsFailure)
            {
                return res;
            }
        }

        return Result.Success();
    }

    private static Result ValidateRule(DataViewFilterNodeDto node)
    {
        return node.Children is { Count: > 0 }
            ? Result.Failure(
                ReportingErrors.Validation(
                    "DataView.RuleCannotHaveChildren",
                    "Leaf rule nodes cannot contain child filters."
                )
            )
            : node.RuleKind switch
            {
                DataViewFilterRuleKind.EntityProperty
                or DataViewFilterRuleKind.JsonbAttribute
                    when string.IsNullOrWhiteSpace(node.FieldKey) => Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.FieldKeyRequired",
                        "Property and attribute filter rules require a FieldKey."
                    )
                ),
                DataViewFilterRuleKind.SpatialRadius => DataViewSpatialMath.ValidateRadiusRule(
                    node.Value,
                    node.SecondaryValue
                ),
                DataViewFilterRuleKind.SpatialPolygon => DataViewSpatialMath.ValidatePolygonRule(
                    node.Value
                ),
                DataViewFilterRuleKind.CrossModulePersonSet
                    when string.IsNullOrWhiteSpace(node.Value) => Result.Failure(
                    ReportingErrors.Validation(
                        "DataView.SetReferenceRequired",
                        "Cross-module person set filters require a source identifier Value."
                    )
                ),
                _ => Result.Success(),
            };
    }
}
