using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Query to list adaptive messages and their prioritized adaptations.</summary>
public sealed record GetAdaptiveMessagesQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<AdaptiveMessageResponse>>;
