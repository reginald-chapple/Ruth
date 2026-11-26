using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class CommerceProductsSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "commerce.products";
    public string ModuleName => "Commerce";
    public string EntityTypeKey => "Ruth.Commerce.Product";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetProductsQuery, IReadOnlyList<ProductResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetProductsQuery(ActiveOnly: true),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(p => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        p.Id,
                        p.Name,
                        Subtitle: $"{p.Kind} · {p.CurrencyCode}",
                        Summary: p.Summary,
                        BodyText: $"{p.Name} {p.Slug} {p.Summary} {p.Description}",
                        Tags: [p.Kind.ToString(), p.Slug],
                        UrlPath: "/commerce/storefront",
                        IsPublic: p.IsPublic,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
