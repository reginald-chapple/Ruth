using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Transports;

internal sealed class GetInMemorySentMessagesQueryHandler(InMemoryCommunicationSink sink)
    : IQueryHandler<GetInMemorySentMessagesQuery, IReadOnlyList<InMemoryCapturedMessageResponse>>
{
    public Task<Result<IReadOnlyList<InMemoryCapturedMessageResponse>>> HandleAsync(
        GetInMemorySentMessagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var items = sink.Query(query.Channel, query.RecipientFilter, query.Limit);
        return Task.FromResult(Result.Success(items));
    }
}
