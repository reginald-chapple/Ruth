namespace Ruth.Modules.Search.Contracts.Querying;

/// <summary>Single ranked full-text &amp; trigram search hit with ts_headline highlighted snippet.</summary>
/// <param name="DocumentId">SearchDocument identifier.</param>
/// <param name="ModuleName">Owning module name.</param>
/// <param name="EntityTypeKey">Entity type key.</param>
/// <param name="EntityId">Target entity identifier.</param>
/// <param name="Title">Document title.</param>
/// <param name="Subtitle">Optional subtitle.</param>
/// <param name="HighlightedSnippet">HTML-safe snippet containing &lt;mark&gt; highlight tags from ts_headline.</param>
/// <param name="UrlPath">Deep-link route to open the item.</param>
/// <param name="Score">Combined PostgreSQL ts_rank_cd + pg_trgm similarity score.</param>
/// <param name="IsPublic">Whether publicly visible.</param>
/// <param name="Tags">Document tags.</param>
/// <param name="Facets">Additional document facets.</param>
public sealed record SearchHitDto(
    Guid DocumentId,
    string ModuleName,
    string EntityTypeKey,
    Guid EntityId,
    string Title,
    string? Subtitle,
    string HighlightedSnippet,
    string? UrlPath,
    double Score,
    bool IsPublic,
    IReadOnlyList<string> Tags,
    IReadOnlyDictionary<string, string> Facets
);
