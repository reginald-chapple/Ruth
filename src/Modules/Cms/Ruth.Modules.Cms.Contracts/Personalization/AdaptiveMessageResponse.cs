namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Represents an adaptive message campaign with prioritized segment-targeted adaptations.</summary>
public sealed record AdaptiveMessageResponse(
    Guid Id,
    string Name,
    string Key,
    string? Description,
    Guid? CategoryId,
    DateTimeOffset? StartDateTimeUtc,
    DateTimeOffset? ExpireDateTimeUtc,
    bool IsActive,
    IReadOnlyList<AdaptiveMessageAdaptationResponse> Adaptations
);
