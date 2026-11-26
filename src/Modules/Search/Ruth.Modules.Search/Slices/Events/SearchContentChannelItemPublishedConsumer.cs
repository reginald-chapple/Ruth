using Ruth.Modules.Cms.Contracts.Events;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Infrastructure.Indexing;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Search.Slices.Events;

internal sealed class SearchContentChannelItemPublishedConsumer(
    SearchDocumentUpsertService upsertService
) : IIntegrationEventConsumer<ContentChannelItemPublishedIntegrationEvent>
{
    public async Task ConsumeAsync(
        ContentChannelItemPublishedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new SearchDocumentUpsertSpec(
            "cms.content-items",
            "Cms",
            "Ruth.Cms.ContentChannelItem",
            integrationEvent.ContentChannelItemId,
            integrationEvent.Title,
            Subtitle: integrationEvent.ItemSlug,
            Summary: $"Published content item ({integrationEvent.ChannelSlug}/{integrationEvent.ItemSlug})",
            Tags: [integrationEvent.ChannelSlug, integrationEvent.ItemSlug, "Content"],
            UrlPath: "/cms/content",
            IsPublic: true
        );
        _ = await upsertService.UpsertBatchAsync([spec], cancellationToken);
    }
}
