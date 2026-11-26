using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Query to retrieve recent persisted interactions with optional channel/person filtering.</summary>
public sealed record GetInteractionsQuery(
    string? ChannelKey = null,
    Guid? PersonId = null,
    int MaxCount = 50
) : IQuery<IReadOnlyList<InteractionResponse>>;
