using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S1CmsEngineEndpoints
{
    public static IEndpointRouteBuilder MapCmsEngineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/cms").WithTags("CMS Engine").RequireAuthorization();
        MapThemes(admin);
        MapSitesAndDomains(admin);
        MapLayouts(admin);
        MapPages(admin);
        W6S1CmsBlocksAndUtilityEndpoints.Map(admin, endpoints);
        return endpoints;
    }

    private static void MapThemes(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/themes",
                static async (bool? includeInactive, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetCmsThemesQuery, IReadOnlyList<CmsThemeResponse>>(
                            new GetCmsThemesQuery(includeInactive == true),
                            ct
                        )
                    )
            )
            .WithName("GetCmsThemes");

        _ = admin
            .MapPost(
                "/themes",
                static async (
                    CreateCmsThemeCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateCmsThemeCommand, CmsThemeResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/themes/{res.Value.Slug}.css", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsTheme");
    }

    private static void MapSitesAndDomains(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/sites",
                static async (
                    CmsSiteType? siteType,
                    bool? includeInactive,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetCmsSitesQuery, IReadOnlyList<CmsSiteResponse>>(
                            new GetCmsSitesQuery(siteType, includeInactive == true),
                            ct
                        )
                    )
            )
            .WithName("GetCmsSites");

        _ = admin
            .MapPost(
                "/sites",
                static async (
                    CreateCmsSiteCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateCmsSiteCommand, CmsSiteResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/sites/{res.Value.Id:D}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsSite");

        _ = admin
            .MapPost(
                "/sites/{siteId:guid}/domains",
                static async (
                    Guid siteId,
                    AddSiteDomainCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddSiteDomainCommand, CmsSiteResponse>(
                            body with
                            {
                                SiteId = siteId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddCmsSiteDomain");
    }

    private static void MapLayouts(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/sites/{siteId:guid}/layouts",
                static async (Guid siteId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetCmsLayoutsQuery, IReadOnlyList<CmsLayoutResponse>>(
                            new GetCmsLayoutsQuery(siteId),
                            ct
                        )
                    )
            )
            .WithName("GetCmsLayouts");

        _ = admin
            .MapPost(
                "/layouts",
                static async (
                    CreateCmsLayoutCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateCmsLayoutCommand, CmsLayoutResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/sites/{res.Value.SiteId:D}/layouts", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsLayout");
    }

    private static void MapPages(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/sites/{siteId:guid}/pages",
                static async (
                    Guid siteId,
                    bool? includeUnpublished,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetCmsPagesQuery, IReadOnlyList<CmsPageResponse>>(
                            new GetCmsPagesQuery(siteId, includeUnpublished != false),
                            ct
                        )
                    )
            )
            .WithName("GetCmsPages");

        _ = admin
            .MapPost(
                "/pages",
                static async (
                    CreateCmsPageCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateCmsPageCommand, CmsPageResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/sites/{res.Value.SiteId:D}/pages", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsPage");
    }
}
