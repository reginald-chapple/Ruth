using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S3CmsPersonalizationEndpoints
{
    public static IEndpointRouteBuilder MapCmsPersonalizationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var admin = endpoints
            .MapGroup("/api/cms/personalization")
            .WithTags("CMS Personalization & Interactions")
            .RequireAuthorization();

        MapInteractionChannels(admin);
        MapInteractionReadAndIngest(admin, endpoints);
        MapSegments(admin);
        MapAdaptiveMessageAdmin(admin);
        MapAdaptiveMessageResolve(endpoints);
        return endpoints;
    }

    private static void MapInteractionChannels(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/channels", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetInteractionChannelsQuery,
                        IReadOnlyList<InteractionChannelResponse>
                    >(new GetInteractionChannelsQuery(), ct)
                )
            )
            .WithName("GetInteractionChannels");

        _ = admin
            .MapPost(
                "/channels",
                static async (
                    CreateInteractionChannelCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateInteractionChannelCommand,
                        InteractionChannelResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created("/api/cms/personalization/channels", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateInteractionChannel");
    }

    private static void MapInteractionReadAndIngest(
        RouteGroupBuilder admin,
        IEndpointRouteBuilder endpoints
    )
    {
        _ = admin
            .MapGet(
                "/interactions",
                static async (
                    string? channelKey,
                    Guid? personId,
                    int? maxCount,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetInteractionsQuery,
                            IReadOnlyList<InteractionResponse>
                        >(new GetInteractionsQuery(channelKey, personId, maxCount ?? 50), ct)
                    )
            )
            .WithName("GetCmsInteractions");

        _ = admin
            .MapPost(
                "/interactions/flush",
                static async (ICommandDispatcher c, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            FlushInteractionBufferCommand,
                            InteractionBufferStatusResponse
                        >(new FlushInteractionBufferCommand(), ct)
                    )
            )
            .WithName("FlushCmsInteractionBuffer");

        _ = endpoints
            .MapPost(
                "/api/cms/public/interactions/ingest",
                static async (
                    IngestInteractionsCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        IngestInteractionsCommand,
                        InteractionBufferStatusResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Accepted("/api/cms/personalization/interactions", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithTags("CMS Public")
            .AllowAnonymous()
            .WithName("IngestCmsInteractions");
    }

    private static void MapSegments(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/segments", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetPersonalizationSegmentsQuery,
                        IReadOnlyList<PersonalizationSegmentResponse>
                    >(new GetPersonalizationSegmentsQuery(), ct)
                )
            )
            .WithName("GetPersonalizationSegments");

        _ = admin
            .MapPost(
                "/segments",
                static async (
                    CreatePersonalizationSegmentCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreatePersonalizationSegmentCommand,
                        PersonalizationSegmentResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created("/api/cms/personalization/segments", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreatePersonalizationSegment");

        _ = admin
            .MapPost(
                "/segments/{segmentIdOrKey}/evaluate",
                static async (string segmentIdOrKey, ICommandDispatcher c, CancellationToken ct) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            EvaluatePersonalizationSegmentCommand,
                            PersonalizationSegmentResponse
                        >(new EvaluatePersonalizationSegmentCommand(segmentIdOrKey), ct)
                    )
            )
            .WithName("EvaluatePersonalizationSegment");
    }

    private static void MapAdaptiveMessageAdmin(RouteGroupBuilder admin)
    {
        _ = admin
            .MapGet("/adaptive-messages", static async (IQueryDispatcher q, CancellationToken ct) =>
                CmsHttpResultMapper.ToHttpResult(
                    await q.QueryAsync<
                        GetAdaptiveMessagesQuery,
                        IReadOnlyList<AdaptiveMessageResponse>
                    >(new GetAdaptiveMessagesQuery(), ct)
                )
            )
            .WithName("GetAdaptiveMessages");

        _ = admin
            .MapPost(
                "/adaptive-messages",
                static async (
                    CreateAdaptiveMessageCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        CreateAdaptiveMessageCommand,
                        AdaptiveMessageResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created("/api/cms/personalization/adaptive-messages", res.Value)
                        : CmsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateAdaptiveMessage");

        _ = admin
            .MapPost(
                "/adaptive-messages/{messageId:guid}/adaptations",
                static async (
                    Guid messageId,
                    AddAdaptiveMessageAdaptationCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await c.SendAsync<
                            AddAdaptiveMessageAdaptationCommand,
                            AdaptiveMessageResponse
                        >(body with { AdaptiveMessageId = messageId }, ct)
                    )
            )
            .WithName("AddAdaptiveMessageAdaptation");
    }

    private static void MapAdaptiveMessageResolve(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapPost(
                "/api/cms/public/adaptive-messages/{messageKey}/resolve",
                static async (
                    string messageKey,
                    Guid? personId,
                    bool? recordView,
                    RequestEvaluationContext? ctx,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    CmsHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            ResolveAdaptiveMessageQuery,
                            ResolvedAdaptiveMessageResponse
                        >(
                            new ResolveAdaptiveMessageQuery(
                                messageKey,
                                personId,
                                ctx,
                                recordView == true
                            ),
                            ct
                        )
                    )
            )
            .WithTags("CMS Public")
            .AllowAnonymous()
            .WithName("ResolveAdaptiveMessage");
    }
}
