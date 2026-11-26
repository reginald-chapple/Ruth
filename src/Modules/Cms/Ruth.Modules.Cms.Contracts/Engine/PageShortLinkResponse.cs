namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a CMS URL shortlink with click analytics and optional UTM attribution.</summary>
public sealed record PageShortLinkResponse(
    Guid Id,
    Guid SiteId,
    string Token,
    string TargetUrl,
    Guid? CategoryId,
    bool IsPinned,
    long ClickCount,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    DateTimeOffset? LastClickedUtc
);
