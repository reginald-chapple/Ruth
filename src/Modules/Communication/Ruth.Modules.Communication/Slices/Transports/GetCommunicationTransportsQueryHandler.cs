using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Transports;

internal sealed class GetCommunicationTransportsQueryHandler(
    CommunicationTransportResolver resolver
)
    : IQueryHandler<
        GetCommunicationTransportsQuery,
        IReadOnlyList<CommunicationTransportStatusResponse>
    >
{
    public Task<Result<IReadOnlyList<CommunicationTransportStatusResponse>>> HandleAsync(
        GetCommunicationTransportsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var all = resolver.GetAllTransports();
        var filtered = query.Channel.HasValue
            ? all.Where(t => t.Channel == query.Channel.Value)
            : all;

        IReadOnlyList<CommunicationTransportStatusResponse> items =
        [
            .. filtered
                .OrderBy(static t => t.Channel)
                .ThenBy(static t => t.TransportKey, StringComparer.OrdinalIgnoreCase)
                .Select(t =>
                {
                    var activeDefault = resolver.ResolveForChannel(t.Channel);
                    return new CommunicationTransportStatusResponse(
                        t.TransportKey,
                        t.DisplayName,
                        t.Channel,
                        t.IsConfigured,
                        string.Equals(
                            t.TransportKey,
                            activeDefault.TransportKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                }),
        ];

        return Task.FromResult(Result.Success(items));
    }
}
