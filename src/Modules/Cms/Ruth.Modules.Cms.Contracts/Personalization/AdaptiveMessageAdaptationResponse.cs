namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Represents a prioritized content adaptation belonging to an AdaptiveMessage.</summary>
public sealed record AdaptiveMessageAdaptationResponse(
    Guid Id,
    Guid AdaptiveMessageId,
    string Name,
    int Order,
    IReadOnlyList<string> SegmentKeys,
    DateTimeOffset? StartDateTimeUtc,
    DateTimeOffset? ExpireDateTimeUtc,
    int? ViewSaturationCount,
    int? ViewSaturationInDays,
    string Headline,
    string BodyMarkup,
    string? CallToActionText,
    string? CallToActionUrl,
    string? ImageUrl,
    bool IsActive
);
