namespace Ruth.Modules.Search.Contracts.Querying;

/// <summary>Facet bucket count returned alongside search hits.</summary>
/// <param name="Value">Facet value (e.g., ModuleName or EntityTypeKey).</param>
/// <param name="Count">Number of matching documents in this bucket.</param>
public sealed record SearchFacetBucketDto(string Value, int Count);
