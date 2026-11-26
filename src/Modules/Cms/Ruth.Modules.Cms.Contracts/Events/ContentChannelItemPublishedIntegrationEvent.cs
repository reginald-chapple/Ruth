using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Cms.Contracts.Events;

/// <summary>Integration event emitted when a CMS content channel item is approved and published.</summary>
public sealed record ContentChannelItemPublishedIntegrationEvent(
    DateTimeOffset OccurredAtUtc,
    Guid ContentChannelId,
    Guid ContentChannelItemId,
    string ChannelSlug,
    string ItemSlug,
    string Title
) : IntegrationEvent(OccurredAtUtc);
