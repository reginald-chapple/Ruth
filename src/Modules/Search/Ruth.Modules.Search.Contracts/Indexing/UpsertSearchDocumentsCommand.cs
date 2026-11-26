using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Contracts.Indexing;

/// <summary>Upserts one or more SearchDocuments directly into the PostgreSQL full-text index.</summary>
/// <param name="Documents">Documents to upsert.</param>
public sealed record UpsertSearchDocumentsCommand(IReadOnlyList<SearchDocumentUpsertSpec> Documents)
    : ICommand<SearchIndexRebuildResponse>;
