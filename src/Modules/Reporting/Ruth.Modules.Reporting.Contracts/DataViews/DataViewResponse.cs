namespace Ruth.Modules.Reporting.Contracts.DataViews;

/// <summary>Response representing a persisted DataView definition and its Query AST.</summary>
/// <param name="Id">DataView identifier.</param>
/// <param name="Name">Human-readable name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="CategoryId">Optional Core CategoryId.</param>
/// <param name="EntityTypeKey">Target entity type key.</param>
/// <param name="TransformKey">Optional post-filter transform key.</param>
/// <param name="IsActive">Whether the DataView is active.</param>
/// <param name="IsSystem">Whether the DataView is system-managed.</param>
/// <param name="PersistedScheduleIntervalMinutes">Optional background materialization interval in minutes.</param>
/// <param name="PersistedLastRefreshDateTimeUtc">Last materialization timestamp in UTC.</param>
/// <param name="PersistedLastRunDurationMilliseconds">Last materialization duration in milliseconds.</param>
/// <param name="PersistedResultCount">Count of currently persisted entity IDs.</param>
/// <param name="RootFilter">Root Query AST filter tree.</param>
public sealed record DataViewResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? CategoryId,
    string EntityTypeKey,
    string? TransformKey,
    bool IsActive,
    bool IsSystem,
    int? PersistedScheduleIntervalMinutes,
    DateTimeOffset? PersistedLastRefreshDateTimeUtc,
    int? PersistedLastRunDurationMilliseconds,
    int PersistedResultCount,
    DataViewFilterNodeDto RootFilter
);
