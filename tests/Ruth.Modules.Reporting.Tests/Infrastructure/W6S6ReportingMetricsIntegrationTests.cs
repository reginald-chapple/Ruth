using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Infrastructure;

[Collection(ReportingFixtures.Name)]
public sealed class W6S6ReportingMetricsIntegrationTests(ReportingContainersFixture containers)
{
    [Fact]
    public async Task Metrics_DataViewCalculation_DynamicChart_And_StarSchema_WorkEndToEnd()
    {
        var conn = await containers.CreateIsolatedDatabaseAsync();
        var aliceId = Guid.CreateVersion7();
        var bobId = Guid.CreateVersion7();

        await using var sp = await ReportingTestHostFactory.CreateAsync(
            conn,
            StubPersonSourceProvider.CreateDefault(aliceId, bobId)
        );
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var dvRes = await commands.SendAsync<CreateDataViewCommand, DataViewResponse>(
            new CreateDataViewCommand(
                "Active Gold Members",
                "Ruth.Crm.Person",
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.JsonbAttribute,
                    "tier",
                    DataViewFilterOperator.Equals,
                    "Gold"
                )
            )
        );
        Assert.True(dvRes.IsSuccess);

        var catRes = await commands.SendAsync<CreateMetricCategoryCommand, MetricCategoryResponse>(
            new CreateMetricCategoryCommand("Membership KPIs")
        );
        Assert.True(catRes.IsSuccess);

        var metricRes = await commands.SendAsync<CreateMetricCommand, MetricResponse>(
            new CreateMetricCommand(
                "Active Gold Count",
                "kpi.active-gold",
                MetricSourceType.DataView,
                CategoryId: catRes.Value.Id,
                DataViewId: dvRes.Value.Id,
                UnitLabel: "Members"
            )
        );
        Assert.True(metricRes.IsSuccess);

        var calcRes = await commands.SendAsync<CalculateMetricCommand, MetricValueResponse>(
            new CalculateMetricCommand(metricRes.Value.Id)
        );
        Assert.True(calcRes.IsSuccess);
        Assert.Equal(1m, calcRes.Value.YValue);

        var chartRes = await queries.QueryAsync<GetDynamicChartQuery, DynamicChartResponse>(
            new GetDynamicChartQuery(metricRes.Value.Id, Granularity: ChartRollupGranularity.Month)
        );
        Assert.True(chartRes.IsSuccess);
        Assert.Equal(1m, chartRes.Value.LatestValue);

        var starRes = await commands.SendAsync<
            RefreshStarSchemaProjectionsCommand,
            StarSchemaRefreshResponse
        >(new RefreshStarSchemaProjectionsCommand());
        Assert.True(starRes.IsSuccess);
        Assert.True(starRes.Value.DimDateRows >= 365);
        Assert.Equal(1, starRes.Value.DimMetricRows);
        Assert.Equal(1, starRes.Value.FactMetricDailyRows);
    }
}
