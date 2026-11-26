namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Request passed to an ISearchDocumentProvider during full or collection reindexing.</summary>
/// <param name="MaxDocuments">Upper bound of documents to load in one pass.</param>
/// <param name="ContentCollectionId">Optional target ContentCollection identifier.</param>
/// <param name="FilterConfigJson">Optional provider-specific JSON filter configuration.</param>
public sealed record SearchProviderLoadRequest(
    int MaxDocuments = 500,
    Guid? ContentCollectionId = null,
    string? FilterConfigJson = null
);
