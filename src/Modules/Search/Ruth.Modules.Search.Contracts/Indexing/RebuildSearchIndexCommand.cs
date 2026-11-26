using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Rebuilds the universal Spotlight index or a specific ContentCollection from registered ISearchDocumentProviders.</summary>
/// <param name="ContentCollectionId">Optional ContentCollection to rebuild (null rebuilds all global providers).</param>
/// <param name="ProviderKey">Optional single provider key filter.</param>
public sealed record RebuildSearchIndexCommand(
    Guid? ContentCollectionId = null,
    string? ProviderKey = null
) : ICommand<SearchIndexRebuildResponse>;
