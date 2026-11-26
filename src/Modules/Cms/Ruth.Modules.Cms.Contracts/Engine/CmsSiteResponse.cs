namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a CMS site with multi-host domains and theme configuration.</summary>
public sealed record CmsSiteResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ThemeId,
    Guid? DefaultPageId,
    Guid? LoginPageId,
    Guid? NotFoundPageId,
    Guid? BranchId,
    CmsSiteType SiteType,
    bool EnablePageViews,
    bool IsActive,
    IReadOnlyList<SiteDomainResponse> Domains,
    IReadOnlyDictionary<string, string?> AttributeValues
);
