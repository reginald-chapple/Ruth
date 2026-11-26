using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class CmsContentItemsSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "cms.content-items";
    public string ModuleName => "Cms";
    public string EntityTypeKey => "Ruth.Cms.ContentChannelItem";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetContentChannelItemsQuery, IReadOnlyList<ContentChannelItemResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetContentChannelItemsQuery(PublishedOnly: true),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(item => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        item.Id,
                        item.Title,
                        Subtitle: item.Slug,
                        Summary: item.Summary,
                        BodyText: item.Content,
                        Tags: [item.Slug, item.Status.ToString()],
                        UrlPath: "/cms/content",
                        IsPublic: item.Status == ContentChannelItemStatus.Approved,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
