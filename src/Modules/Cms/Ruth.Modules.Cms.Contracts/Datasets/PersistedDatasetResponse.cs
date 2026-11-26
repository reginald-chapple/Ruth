namespace Ruth.Modules.Cms.Contracts.Datasets;

/// <summary>Represents a cached JSON persisted dataset consumed by Razor CMS blocks and Minimal APIs.</summary>
public sealed record PersistedDatasetResponse(
    Guid Id,
    string AccessKey,
    string Name,
    string? Description,
    PersistedDatasetSourceKind DataSourceKind,
    IReadOnlyDictionary<string, string?> SourceConfiguration,
    int RefreshIntervalMinutes,
    DateTimeOffset? ExpireDateTimeUtc,
    int MemoryCacheDurationMinutes,
    string? ResultDataJson,
    DateTimeOffset? LastRefreshDateTimeUtc,
    long TimeToBuildMs,
    bool IsActive,
    bool AllowManualRefresh
);
