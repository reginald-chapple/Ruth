using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Friendly URL route template mapped to a <see cref="CmsPage" />.</summary>
public sealed class PageRoute : Entity<PageRouteId>
{
    private PageRoute() { }

    private PageRoute(PageRouteId id, CmsPageId pageId, string route, bool isGlobal)
        : base(id)
    {
        PageId = pageId;
        Route = route;
        IsGlobal = isGlobal;
    }

    public CmsPageId PageId { get; private set; }
    public string Route { get; private set; } = string.Empty;
    public bool IsGlobal { get; private set; }

    public static Result<PageRoute> Create(CmsPageId pageId, string route, bool isGlobal = false)
    {
        if (pageId.IsEmpty)
        {
            return Result.Failure<PageRoute>(
                CmsErrors.Validation("PageRoute.PageRequired", "PageId is required.")
            );
        }

        var normalized = CmsTextNormalizer.NormalizeRoute(route);
        return normalized.Length > 260
            ? Result.Failure<PageRoute>(
                CmsErrors.Validation("PageRoute.TooLong", "Route must be 260 characters or fewer.")
            )
            : Result.Success(new PageRoute(PageRouteId.Create(), pageId, normalized, isGlobal));
    }
}
