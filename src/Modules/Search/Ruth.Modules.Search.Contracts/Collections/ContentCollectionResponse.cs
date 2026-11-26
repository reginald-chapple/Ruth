namespace Ruth.Modules.Search.Contracts.Collections;

/// <summary>Persisted ContentCollection configuration.</summary>
/// <param name="Id">Collection identifier.</param>
/// <param name="Name">Collection display name.</param>
/// <param name="CollectionKey">Unique normalized key/slug.</param>
/// <param name="Description">Optional description.</param>
/// <param name="IsPublic">Whether searchable via public CMS/portal endpoints.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="EnableRequestFilters">Whether CMS request filters apply.</param>
/// <param name="EnableSegments">Whether personalization segments apply.</param>
/// <param name="LastIndexDateTimeUtc">Last reindex timestamp in UTC.</param>
/// <param name="LastIndexItemCount">Number of indexed documents in last run.</param>
/// <param name="Sources">Configured document sources.</param>
public sealed record ContentCollectionResponse(
    Guid Id,
    string Name,
    string CollectionKey,
    string? Description,
    bool IsPublic,
    bool IsActive,
    bool EnableRequestFilters,
    bool EnableSegments,
    DateTimeOffset? LastIndexDateTimeUtc,
    int LastIndexItemCount,
    IReadOnlyList<ContentCollectionSourceResponse> Sources
);
