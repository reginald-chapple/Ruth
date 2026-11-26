using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Analytics;

internal sealed class GetStarSchemaStatusQueryHandler(StarSchemaProjectionRefresher refresher)
    : IQueryHandler<GetStarSchemaStatusQuery, StarSchemaRefreshResponse>
{
    public async Task<Result<StarSchemaRefreshResponse>> HandleAsync(
        GetStarSchemaStatusQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var status = await refresher.GetStatusAsync(cancellationToken);
        return Result.Success(status);
    }
}
