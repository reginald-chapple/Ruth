using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S4CmsChannelsEndpoints
{
    public static IEndpointRouteBuilder MapCmsChannelsEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var admin = endpoints
            .MapGroup("/api/cms")
            .WithTags("CMS Mobile & Signage Admin")
            .RequireAuthorization();

        MapMobileAdminCreateAndRead(admin);
        MapMobileAdminLayoutsAndPages(admin);
        MapMobileAdminDeepLinks(admin);
        MapSignageAdmin(admin);
        MapMobilePublicV1(endpoints);
        MapSignagePublicV1(endpoints);
        return endpoints;
    }

    private static void MapMobileAdminCreateAndRead(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/mobile-apps", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetMobileApplicationsQuery,
                        IReadOnlyList<MobileApplicationResponse>
                    >(new GetMobileApplicationsQuery(), ct)
                )
            )
            .WithName("GetMobileApplications");

        _ = admin
            .MapPost(
                "/mobile-apps",
                static async (
                    CreateMobileApplicationCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateMobileApplicationCommand,
                        MobileApplicationResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/v1/mobile/apps/{res.Value.Slug}/bootstrap",
                            res.Value
                        )
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateMobileApplication");
    }

    private static void MapMobileAdminLayoutsAndPages(RouteGroupBuilder admin)
    {
        _ = admin
            .MapPost(
                "/mobile-apps/{appId:guid}/layouts",
                static async (
                    Guid appId,
                    AddMobileLayoutCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddMobileLayoutCommand, MobileApplicationResponse>(
                            body with
                            {
                                MobileApplicationId = appId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddMobileLayout");

        _ = admin
            .MapPost(
                "/mobile-apps/{appId:guid}/pages",
                static async (
                    Guid appId,
                    AddMobilePageCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddMobilePageCommand, MobileApplicationResponse>(
                            body with
                            {
                                MobileApplicationId = appId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddMobilePage");
    }

    private static void MapMobileAdminDeepLinks(RouteGroupBuilder admin)
    {
        _ = admin
            .MapPost(
                "/mobile-apps/{appId:guid}/deeplinks",
                static async (
                    Guid appId,
                    AddMobileDeepLinkCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<AddMobileDeepLinkCommand, MobileApplicationResponse>(
                            body with
                            {
                                MobileApplicationId = appId,
                            },
                            ct
                        )
                    )
            )
            .WithName("AddMobileDeepLink");
    }

    private static void MapSignageAdmin(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/signage-apps",
                static async (
                    SignagePlatformType? platform,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetSignageApplicationsQuery,
                            IReadOnlyList<SignageApplicationResponse>
                        >(new GetSignageApplicationsQuery(platform), ct)
                    )
            )
            .WithName("GetSignageApplications");

        _ = admin
            .MapPost(
                "/signage-apps",
                static async (
                    CreateSignageApplicationCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateSignageApplicationCommand,
                        SignageApplicationResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/v1/signage/{res.Value.Slug}/feed", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateSignageApplication");
    }

    private static void MapMobilePublicV1(IEndpointRouteBuilder endpoints)
    {
        var mobileV1 = endpoints
            .MapGroup("/api/v1/mobile/apps/{appSlug}")
            .WithTags("Mobile Shell API v1")
            .AllowAnonymous();

        _ = mobileV1
            .MapGet(
                "/bootstrap",
                static async (
                    string appSlug,
                    string? clientVersion,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetMobileAppBootstrapQuery, MobileAppBootstrapResponse>(
                            new GetMobileAppBootstrapQuery(appSlug, clientVersion),
                            ct
                        )
                    )
            )
            .WithName("GetMobileAppBootstrapV1");

        _ = mobileV1
            .MapGet(
                "/pages/{pageKey}",
                static async (
                    string appSlug,
                    string pageKey,
                    Guid? personId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetHydratedMobilePageQuery, HydratedMobilePageResponse>(
                            new GetHydratedMobilePageQuery(appSlug, pageKey, personId),
                            ct
                        )
                    )
            )
            .WithName("GetHydratedMobilePageV1");

        _ = mobileV1
            .MapGet(
                "/deeplinks/resolve",
                static async (
                    string appSlug,
                    string path,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            ResolveMobileDeepLinkQuery,
                            ResolvedMobileDeepLinkResponse
                        >(new ResolveMobileDeepLinkQuery(appSlug, path), ct)
                    )
            )
            .WithName("ResolveMobileDeepLinkV1");
    }

    private static void MapSignagePublicV1(IEndpointRouteBuilder endpoints)
    {
        var signageV1 = endpoints
            .MapGroup("/api/v1/signage/{appSlug}")
            .WithTags("Digital Signage & TV Feeds v1")
            .AllowAnonymous();

        _ = signageV1
            .MapGet(
                "/feed",
                static (
                    string appSlug,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) => RenderSignageFeedAsync(appSlug, "signage", http, q, ct)
            )
            .WithName("GetDigitalSignageFeedV1");

        _ = signageV1
            .MapGet(
                "/apple-tv",
                static (
                    string appSlug,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) => RenderSignageFeedAsync(appSlug, "apple-tv", http, q, ct)
            )
            .WithName("GetAppleTvFeedV1");

        _ = signageV1
            .MapGet(
                "/roku",
                static (
                    string appSlug,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) => RenderSignageFeedAsync(appSlug, "roku", http, q, ct)
            )
            .WithName("GetRokuFeedV1");
    }

    private static async Task<IResult> RenderSignageFeedAsync(
        string appSlug,
        string format,
        HttpContext http,
        IQueryDispatcher q,
        CancellationToken ct
    )
    {
        var origin = $"{http.Request.Scheme}://{http.Request.Host}";
        var res = await q.QueryAsync<GetSignageFeedQuery, SignageFeedResponse>(
            new GetSignageFeedQuery(appSlug, format, origin),
            ct
        );
        return res.IsSuccess
            ? Results.Content(res.Value.PayloadJson, res.Value.ContentType)
            : CmsHttpResultMapper.ToProblem(res.PrimaryError);
    }
}
