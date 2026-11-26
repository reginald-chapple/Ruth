using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Domain.DataViews;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Domain;

public sealed class W6S5DataViewCompilerDomainTests
{
    [Fact]
    public void DataView_Update_FormatScalar_And_SqlCompiler_CoverAllOperators()
    {
        var root = BuildComprehensiveAst();
        var dv = DataView.Create("Dynamic View", "Ruth.Crm.Person", root).Value;
        Assert.True(
            dv.Update(
                "Updated View",
                root,
                description: "Refreshed description",
                categoryId: null,
                transformKey: null,
                persistedScheduleIntervalMinutes: 30,
                isActive: true
            ).IsSuccess
        );
        Assert.Equal("Updated View", dv.Name);
        Assert.True(
            dv.Update(
                "",
                root,
                description: null,
                categoryId: null,
                transformKey: null,
                persistedScheduleIntervalMinutes: null,
                isActive: true
            ).IsFailure
        );

        var sql = DataViewSqlCompiler.Compile(
            root,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Status"] = "status",
                ["Email"] = "email",
                ["Name"] = "name",
                ["Notes"] = "notes",
                ["Phone"] = "phone",
                ["Score"] = "score",
            }
        );
        Assert.Contains("ILIKE", sql.WhereClause, StringComparison.Ordinal);
        Assert.Contains("NOT (", sql.WhereClause, StringComparison.Ordinal);
        Assert.Contains("IS NULL", sql.WhereClause, StringComparison.Ordinal);
        Assert.Contains("IS NOT NULL", sql.WhereClause, StringComparison.Ordinal);
        Assert.Equal(string.Empty, DataViewScalarComparer.FormatScalar(value: null));
        Assert.Equal("true", DataViewScalarComparer.FormatScalar(value: true));
        Assert.Equal("false", DataViewScalarComparer.FormatScalar(value: false));
        Assert.Equal("2026-05-01", DataViewScalarComparer.FormatScalar(new DateOnly(2026, 5, 1)));
        Assert.Contains(
            "2026-05-01",
            DataViewScalarComparer.FormatScalar(
                new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            ),
            StringComparison.Ordinal
        );
        Assert.True(
            DataViewAstValidator
                .Validate(
                    new DataViewFilterNodeDto(
                        DataViewFilterExpressionType.Rule,
                        DataViewFilterRuleKind.SpatialPolygon,
                        Value: "33.0,-113.0"
                    )
                )
                .IsFailure
        );

        VerifyAstValidatorBoundsAndEmptyGroupSql();
    }

    private static void VerifyAstValidatorBoundsAndEmptyGroupSql()
    {
        Assert.True(DataViewAstValidator.Validate(root: null).IsFailure);
        Assert.True(
            DataViewAstValidator
                .Validate(new DataViewFilterNodeDto(DataViewFilterExpressionType.GroupAll))
                .IsSuccess
        );

        // Exceed MaxDepth (8)
        var deep = new DataViewFilterNodeDto(DataViewFilterExpressionType.GroupAll);
        for (var i = 0; i <= DataViewAstValidator.MaxDepth + 1; i++)
        {
            deep = new DataViewFilterNodeDto(
                DataViewFilterExpressionType.GroupAll,
                Children: [deep]
            );
        }

        Assert.True(DataViewAstValidator.Validate(deep).IsFailure);

        // Exceed MaxNodeCount (128)
        var manyChildren = Enumerable
            .Range(0, DataViewAstValidator.MaxNodeCount + 2)
            .Select(static _ => new DataViewFilterNodeDto(
                DataViewFilterExpressionType.Rule,
                DataViewFilterRuleKind.EntityProperty,
                "Status",
                DataViewFilterOperator.IsNotBlank
            ))
            .ToList();
        var wide = new DataViewFilterNodeDto(
            DataViewFilterExpressionType.GroupAll,
            Children: manyChildren
        );
        Assert.True(DataViewAstValidator.Validate(wide).IsFailure);

        // Empty group SQL compilation returns TRUE
        var emptyCompiled = DataViewSqlCompiler.Compile(
            new DataViewFilterNodeDto(DataViewFilterExpressionType.GroupAll),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        );
        Assert.Equal("TRUE", emptyCompiled.WhereClause);
    }

    private static DataViewFilterNodeDto BuildComprehensiveAst()
    {
        return new DataViewFilterNodeDto(
            DataViewFilterExpressionType.GroupAny,
            Children:
            [
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Status",
                    DataViewFilterOperator.NotEquals,
                    "Archived"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Email",
                    DataViewFilterOperator.Contains,
                    "@org"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Name",
                    DataViewFilterOperator.EndsWith,
                    "son"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Notes",
                    DataViewFilterOperator.IsBlank
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Phone",
                    DataViewFilterOperator.IsNotBlank
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.GroupAllFalse,
                    Children:
                    [
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "Score",
                            DataViewFilterOperator.GreaterThanOrEqual,
                            "10"
                        ),
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "Score",
                            DataViewFilterOperator.LessThan,
                            "5"
                        ),
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "Score",
                            DataViewFilterOperator.GreaterThan,
                            "1"
                        ),
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "Score",
                            DataViewFilterOperator.LessThanOrEqual,
                            "99"
                        ),
                    ]
                ),
            ]
        );
    }
}
