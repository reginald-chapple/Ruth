namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a configured block instance placed in a page, layout, or site zone.</summary>
public sealed record CmsBlockResponse(
    Guid Id,
    Guid BlockTypeId,
    string BlockTypeKey,
    string BlockTypeName,
    bool RequiresInteractivity,
    Guid? PageId,
    Guid? LayoutId,
    Guid? SiteId,
    string Zone,
    string Name,
    int Order,
    string? PreHtml,
    string? PostHtml,
    string? CssClass,
    int OutputCacheDurationSeconds,
    bool IsActive,
    IReadOnlyDictionary<string, string?> Settings,
    IReadOnlyDictionary<string, string?> AttributeValues
);
