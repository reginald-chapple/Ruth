using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S1CmsPublicEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        MapPublicResolveAndQr(endpoints);
        MapPublicThemeAndRedirect(endpoints);
    }

    private static void MapPublicResolveAndQr(IEndpointRouteBuilder endpoints)
    {
        var pub = endpoints.MapGroup("/api/cms/public").WithTags("CMS Public").AllowAnonymous();

        _ = pub.MapGet(
                "/resolve",
                static async (
                    string? siteSlug,
                    string? path,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                {
                    var queryDict = http.Request.Query.ToDictionary(
                        static kv => kv.Key,
                        static kv => kv.Value.ToString(),
                        StringComparer.OrdinalIgnoreCase
                    );
                    var res = await q.QueryAsync<ResolveCmsPageQuery, ResolvedCmsPageResponse>(
                        new ResolveCmsPageQuery(http.Request.Host.Host, siteSlug, path, queryDict),
                        ct
                    );
                    return CmsHttpResultMapper.ToHttpResult(res);
                }
            )
            .WithName("ResolveCmsPublicPage");

        _ = pub.MapGet(
                "/shortlinks/{token}/qr.svg",
                static async (
                    string token,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                {
                    var origin = $"{http.Request.Scheme}://{http.Request.Host}";
                    var res = await q.QueryAsync<GetShortLinkQrQuery, ShortLinkQrResponse>(
                        new GetShortLinkQrQuery(token, origin),
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Content(res.Value.QrSvgMarkup, "image/svg+xml; charset=utf-8")
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("GetCmsShortLinkQrSvg");
    }

    private static void MapPublicThemeAndRedirect(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapGet(
                "/api/cms/themes/{slug}.css",
                static async (string slug, IQueryDispatcher q, CancellationToken ct) =>
                {
                    var res = await q.QueryAsync<
                        GetCmsThemesQuery,
                        IReadOnlyList<CmsThemeResponse>
                    >(new GetCmsThemesQuery(IncludeInactive: false), ct);
                    if (res.IsFailure)
                    {
                        return CmsHttpResultMapper.ToProblem(res.PrimaryError);
                    }

                    var theme = res.Value.FirstOrDefault(t =>
                        string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)
                    );
                    return theme is null
                        ? Results.NotFound()
                        : Results.Content(theme.RenderedCssBundle, "text/css; charset=utf-8");
                }
            )
            .WithTags("CMS Public")
            .AllowAnonymous()
            .WithName("GetCmsThemeCss");

        _ = endpoints
            .MapGet(
                "/go/{token}",
                static async (
                    string token,
                    HttpContext http,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<ResolvePageShortLinkCommand, PageShortLinkResponse>(
                        new ResolvePageShortLinkCommand(token, http.Request.Host.Host),
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Redirect(res.Value.TargetUrl, permanent: false)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithTags("CMS Public")
            .AllowAnonymous()
            .WithName("RedirectCmsShortLink");
    }
}
