namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a CMS page with routes, entity contexts, and navigation settings.</summary>
public sealed record CmsPageResponse(
    Guid Id,
    Guid SiteId,
    Guid LayoutId,
    Guid? ParentPageId,
    string InternalName,
    string PageTitle,
    string BrowserTitle,
    string? Description,
    string? IconCssClass,
    int Order,
    PageNavDisplayMode DisplayInNavWhen,
    bool RequiresEncryption,
    int OutputCacheDurationSeconds,
    bool IsPublished,
    IReadOnlyList<PageRouteResponse> Routes,
    IReadOnlyList<PageContextResponse> Contexts,
    IReadOnlyDictionary<string, string?> AttributeValues
);
