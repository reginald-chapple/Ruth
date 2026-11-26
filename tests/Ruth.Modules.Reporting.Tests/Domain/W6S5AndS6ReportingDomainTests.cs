using System.IO.Compression;
using System.Text;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Domain.DataViews;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Domain.Metrics;
using Ruth.Modules.Reporting.Domain.Reports;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Domain;

public sealed class W6S5AndS6ReportingDomainTests
{
    [Fact]
    public void DataViewAstEvaluator_Evaluates_And_Or_Not_Jsonb_Spatial_And_CrossModuleSets()
    {
        var aliceId = Guid.CreateVersion7();
        var bobId = Guid.CreateVersion7();
        var donorSetId = Guid.CreateVersion7();

        var alice = new DataViewEntityRecord(
            aliceId,
            "Ruth.Crm.Person",
            aliceId,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "Alice",
                ["RecordStatus"] = "Active",
                ["Age"] = 34,
                ["JoinedOn"] = new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero),
            },
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["membership_tier"] = "Gold",
                ["volunteer_cleared"] = "true",
            },
            Latitude: 33.4484,
            Longitude: -112.0740
        );

        var bob = new DataViewEntityRecord(
            bobId,
            "Ruth.Crm.Person",
            bobId,
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "Bob",
                ["RecordStatus"] = "Inactive",
                ["Age"] = 19,
            },
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["membership_tier"] = "Bronze",
            },
            Latitude: 40.7128,
            Longitude: -74.0060
        );

        var ast = new DataViewFilterNodeDto(
            DataViewFilterExpressionType.GroupAll,
            Children:
            [
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "RecordStatus",
                    DataViewFilterOperator.Equals,
                    "Active"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    "Age",
                    DataViewFilterOperator.Between,
                    "25",
                    "50"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.JsonbAttribute,
                    "membership_tier",
                    DataViewFilterOperator.In,
                    "Silver, Gold"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.SpatialRadius,
                    Value: "33.4500,-112.0700",
                    SecondaryValue: "5000"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.SpatialPolygon,
                    Value: "33.0,-113.0;34.0,-113.0;34.0,-111.0;33.0,-111.0"
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.CrossModulePersonSet,
                    "dataview",
                    DataViewFilterOperator.Equals,
                    donorSetId.ToString("D")
                ),
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.GroupNone,
                    Children:
                    [
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "FirstName",
                            DataViewFilterOperator.StartsWith,
                            "Z"
                        ),
                    ]
                ),
            ]
        );

        Assert.True(DataViewAstValidator.Validate(ast).IsSuccess);

        var sets = new Dictionary<string, IReadOnlySet<Guid>>(StringComparer.OrdinalIgnoreCase)
        {
            [DataViewAstEvaluator.BuildPersonSetLookupKey("dataview", donorSetId.ToString("D"))] =
                new HashSet<Guid> { aliceId },
        };

        Assert.True(DataViewAstEvaluator.Evaluate(ast, alice, sets));
        Assert.False(DataViewAstEvaluator.Evaluate(ast, bob, sets));

        var compiled = DataViewSqlCompiler.Compile(
            ast,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["RecordStatus"] = "record_status",
                ["Age"] = "age",
                ["FirstName"] = "first_name",
            }
        );
        Assert.Contains("ST_DWithin", compiled.WhereClause, StringComparison.Ordinal);
        Assert.Contains("attribute_values ->>", compiled.WhereClause, StringComparison.Ordinal);
        Assert.Contains("person_id = ANY", compiled.WhereClause, StringComparison.Ordinal);
        Assert.True(compiled.Parameters.Count >= 6);
    }

    [Fact]
    public void DataViewScalarComparer_Handles_Operators_And_AstValidator_Rejects_Invalid_Rules()
    {
        Assert.True(
            DataViewScalarComparer.Evaluate(
                "",
                DataViewFilterOperator.IsBlank,
                expected: null,
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                "hi",
                DataViewFilterOperator.IsNotBlank,
                expected: null,
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                "Phoenix",
                DataViewFilterOperator.Contains,
                "oen",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                "Phoenix",
                DataViewFilterOperator.EndsWith,
                "nix",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                100,
                DataViewFilterOperator.GreaterThan,
                "50",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                50,
                DataViewFilterOperator.GreaterThanOrEqual,
                "50",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                25,
                DataViewFilterOperator.LessThan,
                "50",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                50,
                DataViewFilterOperator.LessThanOrEqual,
                "50",
                secondaryExpected: null
            )
        );
        Assert.True(
            DataViewScalarComparer.Evaluate(
                "abc",
                DataViewFilterOperator.NotEquals,
                "xyz",
                secondaryExpected: null
            )
        );

        var badSpatial = new DataViewFilterNodeDto(
            DataViewFilterExpressionType.Rule,
            DataViewFilterRuleKind.SpatialRadius,
            Value: "not-coords",
            SecondaryValue: "-10"
        );
        Assert.True(DataViewAstValidator.Validate(badSpatial).IsFailure);

        var badField = new DataViewFilterNodeDto(
            DataViewFilterExpressionType.Rule,
            DataViewFilterRuleKind.EntityProperty,
            FieldKey: ""
        );
        Assert.True(DataViewAstValidator.Validate(badField).IsFailure);
    }

    [Fact]
    public void ReportingDocumentPackager_Builds_Valid_Csv_Xlsx_And_Docx()
    {
        string[] cols = ["Full Name", "FormulaTest", "Email"];
        IReadOnlyDictionary<string, string?>[] rows =
        [
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Full Name"] = "Doe, Jane",
                ["FormulaTest"] = "=SUM(A1:A2)",
                ["Email"] = "jane@example.org",
            },
        ];

        var csv = Encoding.UTF8.GetString(ReportingDocumentPackager.BuildCsv(cols, rows));
        Assert.Contains("\"Doe, Jane\"", csv, StringComparison.Ordinal);
        Assert.Contains("'=SUM(A1:A2)", csv, StringComparison.Ordinal);

        var xlsxBytes = ReportingDocumentPackager.BuildExcelXlsx("Members", cols, rows);
        using (var zip = new ZipArchive(new MemoryStream(xlsxBytes), ZipArchiveMode.Read))
        {
            Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
            Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
            Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));
        }

        var docxBytes = ReportingDocumentPackager.BuildWordDocx(
            "Welcome",
            ["Hello Jane\nWelcome!"]
        );
        using (var zip = new ZipArchive(new MemoryStream(docxBytes), ZipArchiveMode.Read))
        {
            Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
            var docEntry = zip.GetEntry("word/document.xml");
            Assert.NotNull(docEntry);
            using var reader = new StreamReader(docEntry.Open());
            var xml = reader.ReadToEnd();
            Assert.Contains("Hello Jane", xml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MetricChartRollupCalculator_Computes_Buckets_Goals_RollingAvg_And_Deltas()
    {
        var branchPartition = new MetricPartitionSpec(
            "Branch",
            "Ruth.Core.Branch",
            IsRequired: true
        );
        var metricRes = Metric.Create(
            "Monthly Attendance",
            "ops.monthly-attendance",
            MetricSourceType.Manual,
            unitLabel: "People",
            partitions: [branchPartition]
        );
        Assert.True(metricRes.IsSuccess);
        var metric = metricRes.Value;
        var partId = metric.Partitions.First().Id.Value;
        var branchId = Guid.CreateVersion7();

        // Required partition validation
        var missingPart = MetricValue.Create(
            metric,
            MetricValueType.Measure,
            100m,
            DateTimeOffset.UtcNow
        );
        Assert.True(missingPart.IsFailure);

        var jan = MetricValue
            .Create(
                metric,
                MetricValueType.Measure,
                100m,
                new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                partitions: [new MetricPartitionValueInput(partId, branchId, "North Branch")]
            )
            .Value;
        var janGoal = MetricValue
            .Create(
                metric,
                MetricValueType.Goal,
                120m,
                new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
                partitions: [new MetricPartitionValueInput(partId, branchId, "North Branch")]
            )
            .Value;
        var feb = MetricValue
            .Create(
                metric,
                MetricValueType.Measure,
                150m,
                new DateTimeOffset(2026, 2, 12, 0, 0, 0, TimeSpan.Zero),
                partitions: [new MetricPartitionValueInput(partId, branchId, "North Branch")]
            )
            .Value;

        var chart = MetricChartRollupCalculator.BuildChart(
            metric,
            [jan, janGoal, feb],
            ChartRollupGranularity.Month
        );

        Assert.Equal(150m, chart.LatestValue);
        Assert.Equal(250m, chart.TotalValue);
        Assert.Equal(50m, chart.PeriodDeltaPercent);
        var series = Assert.Single(chart.Series);
        Assert.Equal("North Branch", series.SeriesName);
        Assert.Equal(2, series.Points.Count);
        Assert.Equal(120m, series.Points[0].GoalValue);
        Assert.Equal(125m, series.Points[1].RollingAverage);
        Assert.Equal(50m, series.Points[1].DeltaPercent);
    }

    [Fact]
    public void Report_And_MergeTemplate_Enforce_Domain_Invariants()
    {
        Assert.True(Report.Create("", "Ruth.Crm.Person", []).IsFailure);
        Assert.True(MergeTemplate.Create("Test", MergeTemplateFormat.Html, "").IsFailure);

        var report = Report.Create(
            "Members",
            "Ruth.Crm.Person",
            [new ReportFieldSpec("reporting.field.property", "First Name")]
        );
        Assert.True(report.IsSuccess);
    }
}
