namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Summary returned after upserting or rebuilding SearchDocuments.</summary>
/// <param name="ProvidersExecuted">Count of ISearchDocumentProviders executed.</param>
/// <param name="DocumentsUpserted">Count of documents inserted or updated.</param>
/// <param name="DocumentsSkippedUnchanged">Count of documents skipped due to matching SHA-256 content hash.</param>
/// <param name="DurationMilliseconds">Total execution time in milliseconds.</param>
public sealed record SearchIndexRebuildResponse(
    int ProvidersExecuted,
    int DocumentsUpserted,
    int DocumentsSkippedUnchanged,
    int DurationMilliseconds
);
