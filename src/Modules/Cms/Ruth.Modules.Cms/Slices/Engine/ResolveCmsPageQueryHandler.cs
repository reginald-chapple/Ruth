using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class ResolveCmsPageQueryHandler(
    CmsDbContext db,
    CmsPageBlocksLoader blocksLoader,
    IClock clock
) : IQueryHandler<ResolveCmsPageQuery, ResolvedCmsPageResponse>
{
    public async Task<Result<ResolvedCmsPageResponse>> HandleAsync(
        ResolveCmsPageQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var site = await ResolveSiteAsync(query, cancellationToken);
        if (site is null)
        {
            return Result.Failure<ResolvedCmsPageResponse>(
                CmsErrors.NotFound("Site", query.SiteSlug ?? query.Host ?? "default")
            );
        }

        var pages = await db
            .Pages.AsNoTracking()
            .Include(static p => p.Routes)
            .Include(static p => p.Contexts)
            .Where(p => p.SiteId == site.Id && (query.IncludeUnpublished || p.IsPublished))
            .ToListAsync(cancellationToken);

        var (matchedPage, matchedRoute, routeParams) = MatchPage(site, pages, query.Path);
        return matchedPage is null
            ? Result.Failure<ResolvedCmsPageResponse>(CmsErrors.NotFound("Page", query.Path ?? "/"))
            : await BuildResolvedPageAsync(
                site,
                matchedPage,
                matchedRoute,
                routeParams,
                query.QueryParameters,
                cancellationToken
            );
    }

    private async Task<Result<ResolvedCmsPageResponse>> BuildResolvedPageAsync(
        CmsSite site,
        CmsPage matchedPage,
        string matchedRoute,
        IReadOnlyDictionary<string, string> routeParams,
        IReadOnlyDictionary<string, string>? queryParams,
        CancellationToken ct
    )
    {
        var layout = await db
            .Layouts.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == matchedPage.LayoutId, ct);
        if (layout is null)
        {
            return Result.Failure<ResolvedCmsPageResponse>(
                CmsErrors.NotFound("Layout", matchedPage.LayoutId.ToString())
            );
        }

        var theme = site.ThemeId is { } themeId
            ? await db.Themes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == themeId, ct)
            : null;

        var (blocksByZone, activeHtml) = await blocksLoader.LoadAsync(
            site.Id,
            layout.Id,
            matchedPage.Id,
            clock.UtcNow,
            ct
        );

        var resolvedContexts = CmsRouteMatcher.ExtractPageContexts(
            matchedPage.Contexts,
            routeParams,
            queryParams
        );

        return Result.Success(
            new ResolvedCmsPageResponse(
                CmsProjectionMapper.ToResponse(site),
                CmsProjectionMapper.ToResponse(layout),
                CmsProjectionMapper.ToResponse(matchedPage),
                theme is null ? null : CmsProjectionMapper.ToResponse(theme),
                matchedRoute,
                routeParams,
                resolvedContexts,
                blocksByZone,
                activeHtml
            )
        );
    }

    private async Task<CmsSite?> ResolveSiteAsync(ResolveCmsPageQuery query, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(query.SiteSlug))
        {
            var slug = CmsTextNormalizer.NormalizeSlug(query.SiteSlug);
            var bySlug = await db
                .Sites.AsNoTracking()
                .Include(static s => s.Domains)
                .FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive, ct);
            if (bySlug is not null)
            {
                return bySlug;
            }
        }

        var host = CmsTextNormalizer.NormalizeHost(query.Host);
        if (host.Length > 0)
        {
            var domain = await db
                .SiteDomains.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Domain == host, ct);
            if (domain is not null)
            {
                var byDomain = await db
                    .Sites.AsNoTracking()
                    .Include(static s => s.Domains)
                    .FirstOrDefaultAsync(s => s.Id == domain.SiteId && s.IsActive, ct);
                if (byDomain is not null)
                {
                    return byDomain;
                }
            }
        }

        return await db
            .Sites.AsNoTracking()
            .Include(static s => s.Domains)
            .Where(static s => s.IsActive && s.SiteType == CmsSiteType.Web)
            .OrderBy(static s => s.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);
    }

    private static (
        CmsPage? Page,
        string MatchedRoute,
        IReadOnlyDictionary<string, string> Parameters
    ) MatchPage(CmsSite site, List<CmsPage> pages, string? rawPath)
    {
        var normalizedPath = CmsTextNormalizer.NormalizeRoute(rawPath);
        CmsPage? bestPage = null;
        var bestRoute = string.Empty;
        var bestScore = -1;
        IReadOnlyDictionary<string, string> bestParams = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var page in pages)
        {
            foreach (var route in page.Routes)
            {
                var match = CmsRouteMatcher.Match(route.Route, normalizedPath);
                if (match.IsMatch && match.SpecificityScore > bestScore)
                {
                    bestScore = match.SpecificityScore;
                    bestPage = page;
                    bestRoute = route.Route;
                    bestParams = match.Parameters;
                }
            }
        }

        if (bestPage is not null)
        {
            return (bestPage, bestRoute, bestParams);
        }

        if (normalizedPath.Length == 0 && site.DefaultPageId is { } defId)
        {
            var defaultPage = pages.FirstOrDefault(p => p.Id == defId);
            if (defaultPage is not null)
            {
                return (defaultPage, string.Empty, bestParams);
            }
        }

        return (null, string.Empty, bestParams);
    }
}
