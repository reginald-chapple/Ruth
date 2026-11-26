using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Slices;

internal static class W6S6ReportingMetricsEndpoints
{
    public static IEndpointRouteBuilder MapReportingMetricsAndAnalyticsEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/reporting")
            .WithTags("Reporting Metrics & Executive Analytics")
            .RequireAuthorization();
        MapMetricCategories(group);
        MapMetrics(group);
        MapMetricValues(group);
        MapDynamicChart(group);
        MapExecutiveDashboards(group);
        MapStarSchema(group);
        return endpoints;
    }

    private static void MapMetricCategories(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/metric-categories", static async (IQueryDispatcher q, CancellationToken ct) =>
                ReportingHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetMetricCategoriesQuery,
                        IReadOnlyList<MetricCategoryResponse>
                    >(new GetMetricCategoriesQuery(), ct)
                )
            )
            .WithName("GetReportingMetricCategories");

        _ = group
            .MapPost(
                "/metric-categories",
                static async (
                    CreateMetricCategoryCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateMetricCategoryCommand,
                        MetricCategoryResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/reporting/metric-categories/{res.Value.Id:D}",
                            res.Value
                        )
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateReportingMetricCategory");
    }

    private static void MapMetrics(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/metrics",
                static async (
                    Guid? categoryId,
                    bool? includeInactive,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetMetricsQuery, IReadOnlyList<MetricResponse>>(
                            new GetMetricsQuery(categoryId, includeInactive == true),
                            ct
                        )
                    )
            )
            .WithName("GetReportingMetrics");

        _ = group
            .MapPost(
                "/metrics",
                static async (
                    CreateMetricCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateMetricCommand, MetricResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/reporting/metrics/{res.Value.Id:D}", res.Value)
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateReportingMetric");
    }

    private static void MapMetricValues(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/metrics/values/batch",
                static async (
                    RecordMetricValuesBatchCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            RecordMetricValuesBatchCommand,
                            IReadOnlyList<MetricValueResponse>
                        >(cmd, ct)
                    )
            )
            .WithName("RecordReportingMetricValuesBatch");

        _ = group
            .MapPost(
                "/metrics/{metricId:guid}/calculate",
                static async (
                    Guid metricId,
                    DateTimeOffset? asOfUtc,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await c.SendAsync<CalculateMetricCommand, MetricValueResponse>(
                            new CalculateMetricCommand(metricId, asOfUtc),
                            ct
                        )
                    )
            )
            .WithName("CalculateReportingMetric");

        _ = group
            .MapGet(
                "/metrics/{metricId:guid}/values",
                static async (
                    Guid metricId,
                    DateTimeOffset? startUtc,
                    DateTimeOffset? endUtc,
                    Guid? partitionEntityId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetMetricValuesQuery,
                            IReadOnlyList<MetricValueResponse>
                        >(
                            new GetMetricValuesQuery(metricId, startUtc, endUtc, partitionEntityId),
                            ct
                        )
                    )
            )
            .WithName("GetReportingMetricValues");
    }

    private static void MapDynamicChart(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/metrics/{metricId:guid}/chart",
                static async (
                    Guid metricId,
                    DateTimeOffset? startUtc,
                    DateTimeOffset? endUtc,
                    ChartRollupGranularity? granularity,
                    Guid? partitionEntityId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetDynamicChartQuery, DynamicChartResponse>(
                            new GetDynamicChartQuery(
                                metricId,
                                startUtc,
                                endUtc,
                                granularity ?? ChartRollupGranularity.Month,
                                partitionEntityId
                            ),
                            ct
                        )
                    )
            )
            .WithName("GetReportingDynamicChart");
    }

    private static void MapExecutiveDashboards(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/analytics/revenue-impact",
                static async (
                    DateTimeOffset? startUtc,
                    DateTimeOffset? endUtc,
                    Guid? branchId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetRevenueAndImpactOverviewQuery,
                            RevenueAndImpactOverviewResponse
                        >(new GetRevenueAndImpactOverviewQuery(startUtc, endUtc, branchId), ct)
                    )
            )
            .WithName("GetReportingRevenueAndImpactOverview");

        _ = group
            .MapGet(
                "/analytics/executive-insights",
                static async (
                    int? windowDays,
                    Guid? branchId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetExecutiveInsightsQuery, ExecutiveInsightsResponse>(
                            new GetExecutiveInsightsQuery(windowDays ?? 30, branchId),
                            ct
                        )
                    )
            )
            .WithName("GetReportingExecutiveInsights");
    }

    private static void MapStarSchema(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/analytics/star-schema",
                static async (IQueryDispatcher q, CancellationToken ct) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetStarSchemaStatusQuery, StarSchemaRefreshResponse>(
                            new GetStarSchemaStatusQuery(),
                            ct
                        )
                    )
            )
            .WithName("GetReportingStarSchemaStatus");

        _ = group
            .MapPost(
                "/analytics/star-schema/refresh",
                static async (ICommandDispatcher c, CancellationToken ct) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            RefreshStarSchemaProjectionsCommand,
                            StarSchemaRefreshResponse
                        >(new RefreshStarSchemaProjectionsCommand(), ct)
                    )
            )
            .WithName("RefreshReportingStarSchema");
    }
}
