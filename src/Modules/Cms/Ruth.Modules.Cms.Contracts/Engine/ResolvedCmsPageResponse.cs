namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Result of resolving a host/path against CMS sites, layouts, pages, zones, blocks, and theme CSS.</summary>
public sealed record ResolvedCmsPageResponse(
    CmsSiteResponse Site,
    CmsLayoutResponse Layout,
    CmsPageResponse Page,
    CmsThemeResponse? Theme,
    string MatchedRoute,
    IReadOnlyDictionary<string, string> RouteParameters,
    IReadOnlyDictionary<string, string> ResolvedContexts,
    IReadOnlyDictionary<string, IReadOnlyList<CmsBlockResponse>> BlocksByZone,
    IReadOnlyDictionary<Guid, HtmlContentResponse> ActiveHtmlByBlockId
);
