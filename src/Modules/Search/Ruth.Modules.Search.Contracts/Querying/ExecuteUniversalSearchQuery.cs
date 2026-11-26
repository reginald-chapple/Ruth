using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Contracts.Querying;

/// <summary>Executes deterministic PostgreSQL tsvector + pg_trgm search with query-time ISecured ACL and facet filtering.</summary>
/// <param name="QueryText">User search query (parsed via websearch_to_tsquery + trigram similarity).</param>
/// <param name="Subject">Optional SecuritySubject evaluated against ISecured ACLs (defaults to Anonymous when null).</param>
/// <param name="CollectionKey">Optional ContentCollection slug key scope.</param>
/// <param name="ModuleNames">Optional module facet filters (e.g., Crm, Groups, Lms).</param>
/// <param name="EntityTypeKeys">Optional entity type facet filters.</param>
/// <param name="PublicOnly">When true, restricts results to IsPublic = true documents.</param>
/// <param name="Limit">Maximum hits returned (1-100).</param>
public sealed record ExecuteUniversalSearchQuery(
    string QueryText,
    SecuritySubject? Subject = null,
    string? CollectionKey = null,
    IReadOnlyList<string>? ModuleNames = null,
    IReadOnlyList<string>? EntityTypeKeys = null,
    bool PublicOnly = false,
    int Limit = 20
) : IQuery<SearchResultsResponse>;
