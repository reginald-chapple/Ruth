using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.Analytics;

internal sealed class RefreshStarSchemaProjectionsCommandHandler(
    StarSchemaProjectionRefresher refresher
) : ICommandHandler<RefreshStarSchemaProjectionsCommand, StarSchemaRefreshResponse>
{
    public async Task<Result<StarSchemaRefreshResponse>> HandleAsync(
        RefreshStarSchemaProjectionsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var refreshed = await refresher.RefreshAsync(cancellationToken);
        return Result.Success(refreshed);
    }
}
