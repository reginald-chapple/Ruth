namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Represents a CMS content channel (articles, announcements, stories, resources).</summary>
public sealed record ContentChannelResponse(
    Guid Id,
    Guid ContentChannelTypeId,
    string Name,
    string Slug,
    string? Description,
    string? IconCssClass,
    bool RequiresApproval,
    bool EnableRss,
    string? ChannelUrl,
    string? ItemUrl,
    int TimeToLiveMinutes,
    bool ItemsManuallyOrdered,
    bool IsStructuredContent,
    bool IsActive,
    IReadOnlyList<Guid> ChildChannelIds,
    IReadOnlyDictionary<string, string?> AttributeValues
);
