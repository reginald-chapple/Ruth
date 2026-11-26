namespace Ruth.Modules.Search.Contracts.Collections;

/// <summary>Persisted source attached to a ContentCollection.</summary>
/// <param name="Id">Source identifier.</param>
/// <param name="ProviderKey">Provider key.</param>
/// <param name="EntityTypeKey">Entity type key.</param>
/// <param name="OccurrencesToShow">Max items to index.</param>
/// <param name="CustomBoostWeight">Ranking boost multiplier.</param>
/// <param name="FilterConfigJson">Optional filter JSON.</param>
/// <param name="IsActive">Active flag.</param>
public sealed record ContentCollectionSourceResponse(
    Guid Id,
    string ProviderKey,
    string EntityTypeKey,
    int OccurrencesToShow,
    double CustomBoostWeight,
    string? FilterConfigJson,
    bool IsActive
);
