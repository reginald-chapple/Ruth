namespace Ruth.Modules.Search.Contracts.Querying;

/// <summary>Search results payload with ranked hits, module/entity facets, and execution latency.</summary>
/// <param name="QueryText">Normalized search text.</param>
/// <param name="TotalAuthorizedHits">Total hits returned after ISecured ACL filtering.</param>
/// <param name="ExecutionMilliseconds">Query latency in milliseconds.</param>
/// <param name="Hits">Ranked search hits.</param>
/// <param name="ModuleFacets">Facet counts grouped by ModuleName.</param>
/// <param name="EntityTypeFacets">Facet counts grouped by EntityTypeKey.</param>
public sealed record SearchResultsResponse(
    string QueryText,
    int TotalAuthorizedHits,
    int ExecutionMilliseconds,
    IReadOnlyList<SearchHitDto> Hits,
    IReadOnlyList<SearchFacetBucketDto> ModuleFacets,
    IReadOnlyList<SearchFacetBucketDto> EntityTypeFacets
);
