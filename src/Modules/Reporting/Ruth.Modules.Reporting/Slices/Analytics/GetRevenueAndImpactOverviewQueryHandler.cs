using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Slices.Analytics;

internal sealed class GetRevenueAndImpactOverviewQueryHandler(
    ReportingCrossSchemaAnalyticsReader reader,
    IClock clock
) : IQueryHandler<GetRevenueAndImpactOverviewQuery, RevenueAndImpactOverviewResponse>
{
    public async Task<Result<RevenueAndImpactOverviewResponse>> HandleAsync(
        GetRevenueAndImpactOverviewQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var endUtc = (query.EndDateUtc ?? clock.UtcNow).ToUniversalTime();
        var startUtc = (query.StartDateUtc ?? endUtc.AddMonths(-6)).ToUniversalTime();
        var overview = await reader.LoadRevenueAndImpactAsync(
            startUtc,
            endUtc,
            query.BranchId,
            cancellationToken
        );
        return Result.Success(overview);
    }
}
