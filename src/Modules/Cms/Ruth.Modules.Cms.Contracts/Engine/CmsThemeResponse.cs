namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a CMS CSS custom-property theme bundle.</summary>
public sealed record CmsThemeResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyDictionary<string, string> CssCustomProperties,
    string? CustomCss,
    string RenderedCssBundle,
    bool IsActive,
    bool IsSystem
);
