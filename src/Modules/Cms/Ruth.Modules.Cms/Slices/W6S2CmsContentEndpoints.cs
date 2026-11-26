using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S2CmsContentEndpoints
{
    public static IEndpointRouteBuilder MapCmsContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints
            .MapGroup("/api/cms")
            .WithTags("CMS Content & Media")
            .RequireAuthorization();
        MapChannelTypes(admin);
        MapChannels(admin);
        MapTopics(admin);
        MapChannelItemReadsAndCreate(admin);
        MapChannelItemModerationAndAssociations(admin);
        MapPublicFeeds(endpoints);
        W6S2CmsMediaAndDatasetEndpoints.Map(admin, endpoints);
        return endpoints;
    }

    private static void MapChannelTypes(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/channel-types", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetContentChannelTypesQuery,
                        IReadOnlyList<ContentChannelTypeResponse>
                    >(new GetContentChannelTypesQuery(), ct)
                )
            )
            .WithName("GetContentChannelTypes");

        _ = admin
            .MapPost(
                "/channel-types",
                static async (
                    CreateContentChannelTypeCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateContentChannelTypeCommand,
                        ContentChannelTypeResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created("/api/cms/channel-types", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateContentChannelType");
    }

    private static void MapChannels(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/channels",
                static async (bool? includeInactive, IQueryDispatcher q, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetContentChannelsQuery,
                            IReadOnlyList<ContentChannelResponse>
                        >(new GetContentChannelsQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetContentChannels");

        _ = admin
            .MapPost(
                "/channels",
                static async (
                    CreateContentChannelCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateContentChannelCommand,
                        ContentChannelResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/channels/{res.Value.Slug}/items", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateContentChannel");
    }

    private static void MapTopics(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/topics", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<GetContentTopicsQuery, IReadOnlyList<ContentTopicResponse>>(
                        new GetContentTopicsQuery(),
                        ct
                    )
                )
            )
            .WithName("GetContentTopics");

        _ = admin
            .MapPost(
                "/topics",
                static async (
                    CreateContentTopicCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateContentTopicCommand, ContentTopicResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created("/api/cms/topics", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateContentTopic");
    }

    private static void MapChannelItemReadsAndCreate(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet(
                "/channels/items",
                static async (
                    Guid? channelId,
                    string? channelSlug,
                    Guid? topicId,
                    bool? publishedOnly,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetContentChannelItemsQuery,
                            IReadOnlyList<ContentChannelItemResponse>
                        >(
                            new GetContentChannelItemsQuery(
                                channelId,
                                channelSlug,
                                topicId,
                                publishedOnly == true
                            ),
                            ct
                        )
                    )
            )
            .WithName("GetContentChannelItems");

        _ = admin
            .MapPost(
                "/channels/items",
                static async (
                    CreateContentChannelItemCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateContentChannelItemCommand,
                        ContentChannelItemResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/cms/channels/items/{res.Value.Id:D}", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateContentChannelItem");
    }

    private static void MapChannelItemModerationAndAssociations(RouteGroupBuilder admin)
    {
        _ = admin
            .MapPost(
                "/channels/items/{itemId:guid}/moderate",
                static async (
                    Guid itemId,
                    ModerateContentChannelItemCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            ModerateContentChannelItemCommand,
                            ContentChannelItemResponse
                        >(body with { ItemId = itemId }, ct)
                    )
            )
            .WithName("ModerateContentChannelItem");

        _ = admin
            .MapPost(
                "/channels/items/{parentItemId:guid}/associations",
                static async (
                    Guid parentItemId,
                    AssociateContentChannelItemsCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            AssociateContentChannelItemsCommand,
                            ContentChannelItemResponse
                        >(body with { ParentItemId = parentItemId }, ct)
                    )
            )
            .WithName("AssociateContentChannelItems");
    }

    private static void MapPublicFeeds(IEndpointRouteBuilder endpoints)
    {
        var pub = endpoints.MapGroup("/api/cms").WithTags("CMS Feeds & Datasets").AllowAnonymous();

        _ = pub.MapGet(
                "/channels/{channelSlug}/feed.rss",
                static async (
                    string channelSlug,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                {
                    var origin = $"{http.Request.Scheme}://{http.Request.Host}";
                    var res = await q.QueryAsync<
                        GetContentChannelFeedQuery,
                        ContentChannelFeedResponse
                    >(new GetContentChannelFeedQuery(channelSlug, "rss", origin), ct);
                    return res.IsSuccess
                        ? Results.Content(res.Value.Payload, res.Value.ContentType)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("GetContentChannelRssFeed");

        _ = pub.MapGet(
                "/channels/{channelSlug}/feed.json",
                static async (
                    string channelSlug,
                    HttpContext http,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                {
                    var origin = $"{http.Request.Scheme}://{http.Request.Host}";
                    var res = await q.QueryAsync<
                        GetContentChannelFeedQuery,
                        ContentChannelFeedResponse
                    >(new GetContentChannelFeedQuery(channelSlug, "json", origin), ct);
                    return res.IsSuccess
                        ? Results.Content(res.Value.Payload, res.Value.ContentType)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("GetContentChannelJsonFeed");
    }
}
