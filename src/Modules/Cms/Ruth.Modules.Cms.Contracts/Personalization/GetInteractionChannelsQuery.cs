using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Query to list configured interaction channels.</summary>
public sealed record GetInteractionChannelsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<InteractionChannelResponse>>;
