namespace Ruth.Modules.Search.Contracts.Collections;

/// <summary>Input specification for adding a module provider source to a ContentCollection.</summary>
/// <param name="ProviderKey">ISearchDocumentProvider key (e.g., cms.content-items, lms.courses).</param>
/// <param name="OccurrencesToShow">Maximum occurrences/items pulled per pass.</param>
/// <param name="CustomBoostWeight">Optional ranking multiplier (default 1.0).</param>
/// <param name="FilterConfigJson">Optional JSON filter specification.</param>
public sealed record ContentCollectionSourceSpec(
    string ProviderKey,
    int OccurrencesToShow = 100,
    double CustomBoostWeight = 1.0,
    string? FilterConfigJson = null
);
