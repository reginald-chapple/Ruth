using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Transports;

/// <summary>Queries messages captured by the development in-memory sink and push notification stub.</summary>
/// <param name="Channel">Optional channel filter.</param>
/// <param name="RecipientFilter">Optional substring filter on recipient address.</param>
/// <param name="Limit">Maximum number of recent messages to return (1–200).</param>
public sealed record GetInMemorySentMessagesQuery(
    CommunicationChannel? Channel = null,
    string? RecipientFilter = null,
    int Limit = 50
) : IQuery<IReadOnlyList<InMemoryCapturedMessageResponse>>;
