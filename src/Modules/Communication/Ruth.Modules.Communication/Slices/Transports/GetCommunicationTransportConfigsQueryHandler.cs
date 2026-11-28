using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Transports;

internal sealed class GetCommunicationTransportConfigsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<
        GetCommunicationTransportConfigsQuery,
        IReadOnlyList<CommunicationTransportConfigResponse>
    >
{
    public async Task<Result<IReadOnlyList<CommunicationTransportConfigResponse>>> HandleAsync(
        GetCommunicationTransportConfigsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var rowsQuery = dbContext.CommunicationTransportConfigs.AsNoTracking();
        if (query.Channel.HasValue)
        {
            rowsQuery = rowsQuery.Where(c => c.Channel == query.Channel.Value);
        }

        var rows = await rowsQuery
            .OrderBy(static c => c.Channel)
            .ThenBy(static c => c.TransportKey)
            .ToListAsync(cancellationToken);

        IReadOnlyList<CommunicationTransportConfigResponse> items =
        [
            .. rows.Select(UpsertCommunicationTransportConfigCommandHandler.ToMaskedResponse),
        ];
        return Result.Success(items);
    }
}
