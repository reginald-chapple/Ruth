using Ruth.Modules.Search.Contracts.Indexing;

namespace Ruth.Modules.Search.Tests.Fixtures;

internal sealed class FakeSearchDocumentProvider(
    string providerKey,
    string moduleName,
    string entityTypeKey,
    IReadOnlyList<SearchDocumentUpsertSpec> documents
) : ISearchDocumentProvider
{
    public string ProviderKey => providerKey;
    public string ModuleName => moduleName;
    public string EntityTypeKey => entityTypeKey;

    public Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<SearchDocumentUpsertSpec> results =
        [
            .. documents
                .Take(request.MaxDocuments)
                .Select(d => d with { ContentCollectionId = request.ContentCollectionId }),
        ];
        return Task.FromResult(results);
    }
}
