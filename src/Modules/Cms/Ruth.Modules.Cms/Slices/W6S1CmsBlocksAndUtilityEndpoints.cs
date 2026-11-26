using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S1CmsBlocksAndUtilityEndpoints
{
    public static void Map(RouteGroupBuilder admin, IEndpointRouteBuilder endpoints)
    {
        MapBlockTypes(admin);
        MapBlocks(admin);
        MapHtmlContent(admin);
        MapShortLinks(admin);
        MapRequestFilters(admin);
        W6S1CmsPublicEndpoints.Map(endpoints);
    }

    private static void MapBlockTypes(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/block-types", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<GetCmsBlockTypesQuery, IReadOnlyList<CmsBlockTypeResponse>>(
                        new GetCmsBlockTypesQuery(),
                        ct
                    )
                )
            )
            .WithName("GetCmsBlockTypes");

        _ = admin
            .MapPost(
                "/block-types",
                static async (
                    RegisterCmsBlockTypeCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<RegisterCmsBlockTypeCommand, CmsBlockTypeResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created("/api/cms/block-types", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("RegisterCmsBlockType");
    }

    private static void MapBlocks(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/blocks",
                static async (
                    Guid? pageId,
                    Guid? layoutId,
                    Guid? siteId,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetCmsBlocksQuery, IReadOnlyList<CmsBlockResponse>>(
                            new GetCmsBlocksQuery(pageId, layoutId, siteId),
                            ct
                        )
                    )
            )
            .WithName("GetCmsBlocks");

        _ = admin
            .MapPost(
                "/blocks",
                static async (
                    CreateCmsBlockCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateCmsBlockCommand, CmsBlockResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/blocks/{res.Value.Id:D}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsBlock");

        _ = admin
            .MapPut(
                "/blocks/{blockId:guid}",
                static async (
                    Guid blockId,
                    UpdateCmsBlockCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<UpdateCmsBlockCommand, CmsBlockResponse>(
                            body with
                            {
                                BlockId = blockId,
                            },
                            ct
                        )
                    )
            )
            .WithName("UpdateCmsBlock");
    }

    private static void MapHtmlContent(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/blocks/{blockId:guid}/html",
                static async (Guid blockId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetHtmlContentVersionsQuery,
                            IReadOnlyList<HtmlContentResponse>
                        >(new GetHtmlContentVersionsQuery(blockId), ct)
                    )
            )
            .WithName("GetCmsHtmlContentVersions");

        _ = admin
            .MapPost(
                "/blocks/{blockId:guid}/html",
                static async (
                    Guid blockId,
                    SaveHtmlContentVersionCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<SaveHtmlContentVersionCommand, HtmlContentResponse>(
                        body with
                        {
                            BlockId = blockId,
                        },
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/blocks/{blockId:D}/html", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("SaveCmsHtmlContentVersion");

        _ = admin
            .MapPost(
                "/html/{htmlContentId:guid}/approve",
                static async (
                    Guid htmlContentId,
                    ApproveHtmlContentCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<ApproveHtmlContentCommand, HtmlContentResponse>(
                            body with
                            {
                                HtmlContentId = htmlContentId,
                            },
                            ct
                        )
                    )
            )
            .WithName("ApproveCmsHtmlContent");
    }

    private static void MapShortLinks(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/shortlinks",
                static async (Guid? siteId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetPageShortLinksQuery,
                            IReadOnlyList<PageShortLinkResponse>
                        >(new GetPageShortLinksQuery(siteId), ct)
                    )
            )
            .WithName("GetCmsShortLinks");

        _ = admin
            .MapPost(
                "/shortlinks",
                static async (
                    CreatePageShortLinkCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreatePageShortLinkCommand, PageShortLinkResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created($"/go/{res.Value.Token}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsShortLink");
    }

    private static void MapRequestFilters(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/request-filters",
                static async (Guid? siteId, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetRequestFiltersQuery,
                            IReadOnlyList<RequestFilterResponse>
                        >(new GetRequestFiltersQuery(siteId, IncludeInactive: true), ct)
                    )
            )
            .WithName("GetCmsRequestFilters");

        _ = admin
            .MapPost(
                "/request-filters",
                static async (
                    CreateRequestFilterCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateRequestFilterCommand, RequestFilterResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created("/api/cms/request-filters", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateCmsRequestFilter");

        _ = admin
            .MapPost(
                "/request-filters/{filterIdOrKey}/evaluate",
                static async (
                    string filterIdOrKey,
                    RequestEvaluationContext ctx,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            EvaluateRequestFilterQuery,
                            RequestFilterEvaluationResponse
                        >(new EvaluateRequestFilterQuery(filterIdOrKey, ctx), ct)
                    )
            )
            .WithName("EvaluateCmsRequestFilter");
    }
}
