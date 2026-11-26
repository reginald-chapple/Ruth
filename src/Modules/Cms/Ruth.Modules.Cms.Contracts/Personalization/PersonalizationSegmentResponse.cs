namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Represents a personalization segment backed by Redis bitset/set caching.</summary>
public sealed record PersonalizationSegmentResponse(
    Guid Id,
    string Name,
    string SegmentKey,
    string? Description,
    int TimeToUpdateIntervalMinutes,
    bool IsActive,
    bool IsDirty,
    PersonalizationSegmentFilterSpec FilterSpec,
    DateTimeOffset? LastEvaluatedUtc,
    int CachedMemberCount
);
