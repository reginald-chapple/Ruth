using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Slices.Analytics;

internal sealed class GetExecutiveInsightsQueryHandler(
    ReportingCrossSchemaAnalyticsReader reader,
    StarSchemaProjectionRefresher starSchema,
    IClock clock
) : IQueryHandler<GetExecutiveInsightsQuery, ExecutiveInsightsResponse>
{
    public async Task<Result<ExecutiveInsightsResponse>> HandleAsync(
        GetExecutiveInsightsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = clock.UtcNow;
        var windowDays = Math.Clamp(query.WindowDays, 7, 365);
        var kpis = await reader.LoadExecutiveKpisAsync(
            nowUtc,
            windowDays,
            query.BranchId,
            cancellationToken
        );
        var status = await starSchema.GetStatusAsync(cancellationToken);
        var totalFacts =
            status.FactMetricDailyRows
            + status.FactRevenueMonthlyRows
            + status.FactAttendanceWeeklyRows;

        return Result.Success(
            new ExecutiveInsightsResponse(
                nowUtc,
                windowDays,
                query.BranchId,
                kpis,
                totalFacts > 0 ? status.RefreshedAtUtc : null,
                totalFacts
            )
        );
    }
}
