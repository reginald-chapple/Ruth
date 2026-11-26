using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Contracts.Webhooks;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.VirtualRooms.Slices;

internal static class W5S7VirtualRoomsEndpoints
{
    public static IEndpointRouteBuilder MapVirtualRoomsEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/virtual-rooms")
            .WithTags("Virtual Rooms")
            .RequireAuthorization();

        MapRoomReadRoutes(group);
        MapRoomWriteRoutes(group);
        MapSessionRoutes(group);
        MapJoinTokenRoute(group);
        MapRecordingRoutes(group);
        MapWebhookRoutes(endpoints);
        return endpoints;
    }

    private static void MapRoomReadRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "",
                static async (
                    VirtualRoomContextKind? contextKind,
                    bool? includeInactive,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetVirtualRoomsQuery,
                            IReadOnlyList<VirtualRoomResponse>
                        >(new GetVirtualRoomsQuery(contextKind, includeInactive == true), ct)
                    )
            )
            .WithName("GetVirtualRooms")
            .WithSummary("Lists embedded LiveKit VirtualRooms");

        _ = group
            .MapGet(
                "/{idOrSlug}",
                static async (string idOrSlug, IQueryDispatcher queries, CancellationToken ct) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetVirtualRoomByIdOrSlugQuery,
                            VirtualRoomResponse
                        >(new GetVirtualRoomByIdOrSlugQuery(idOrSlug), ct)
                    )
            )
            .WithName("GetVirtualRoomByIdOrSlug")
            .WithSummary("Gets a VirtualRoom by UUID or slug");
    }

    private static void MapRoomWriteRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "",
                static async (
                    VirtualRoomSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        CreateVirtualRoomCommand,
                        VirtualRoomResponse
                    >(new CreateVirtualRoomCommand(spec), ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/virtual-rooms/{res.Value.Id:D}", res.Value)
                        : VirtualRoomsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateVirtualRoom")
            .WithSummary("Creates an embedded LiveKit VirtualRoom");

        _ = group
            .MapPut(
                "/{id:guid}",
                static async (
                    Guid id,
                    VirtualRoomSpec spec,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<UpdateVirtualRoomCommand, VirtualRoomResponse>(
                            new UpdateVirtualRoomCommand(id, spec),
                            ct
                        )
                    )
            )
            .WithName("UpdateVirtualRoom")
            .WithSummary("Updates a VirtualRoom");
    }

    private static void MapSessionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/sessions",
                static async (
                    Guid? virtualRoomId,
                    VirtualRoomSessionStatus? status,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetVirtualRoomSessionsQuery,
                            IReadOnlyList<VirtualRoomSessionResponse>
                        >(new GetVirtualRoomSessionsQuery(virtualRoomId, status), ct)
                    )
            )
            .WithName("GetVirtualRoomSessions")
            .WithSummary("Lists VirtualRoomSessions with participant dwell and recordings");

        _ = group
            .MapPost(
                "/sessions",
                static async (
                    CreateVirtualRoomSessionCommand cmd,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var res = await commands.SendAsync<
                        CreateVirtualRoomSessionCommand,
                        VirtualRoomSessionResponse
                    >(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/virtual-rooms/sessions?virtualRoomId={res.Value.VirtualRoomId:D}",
                            res.Value
                        )
                        : VirtualRoomsHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateVirtualRoomSession")
            .WithSummary("Creates or starts a VirtualRoomSession");

        _ = group
            .MapPost(
                "/sessions/{sessionId:guid}/close",
                static async (Guid sessionId, ICommandDispatcher commands, CancellationToken ct) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            CloseVirtualRoomSessionCommand,
                            VirtualRoomSessionResponse
                        >(new CloseVirtualRoomSessionCommand(sessionId), ct)
                    )
            )
            .WithName("CloseVirtualRoomSession")
            .WithSummary("Closes a VirtualRoomSession and finalizes auto-attendance");
    }

    private static void MapJoinTokenRoute(RouteGroupBuilder group)
    {
        _ = group
            .MapPost(
                "/{idOrSlug}/join-token",
                static async (
                    string idOrSlug,
                    MintVirtualRoomJoinTokenCommand body,
                    HttpContext httpContext,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    var subject = BuildSubjectFromPrincipal(httpContext.User);
                    var res = await commands.SendAsync<
                        MintVirtualRoomJoinTokenCommand,
                        VirtualRoomJoinSessionResponse
                    >(body with { RoomIdOrSlug = idOrSlug, Subject = body.Subject ?? subject }, ct);
                    return VirtualRoomsHttpResultMapper.ToHttpResult(res);
                }
            )
            .WithName("MintVirtualRoomJoinToken")
            .WithSummary("Mints a short-lived ACL-mapped HMAC-SHA256 LiveKit room JWT");
    }

    private static void MapRecordingRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/recordings",
                static async (
                    Guid? virtualRoomId,
                    bool? completedOnly,
                    IQueryDispatcher queries,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await queries.QueryAsync<
                            GetVirtualRoomRecordingsQuery,
                            IReadOnlyList<VirtualRoomRecordingArchiveResponse>
                        >(
                            new GetVirtualRoomRecordingsQuery(virtualRoomId, completedOnly == true),
                            ct
                        )
                    )
            )
            .WithName("GetVirtualRoomRecordings")
            .WithSummary("Lists RustFS S3 recording archives");

        _ = group
            .MapPost(
                "/sessions/{sessionId:guid}/recordings/start",
                static async (
                    Guid sessionId,
                    StartVirtualRoomRecordingCommand body,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            StartVirtualRoomRecordingCommand,
                            VirtualRoomRecordingArchiveResponse
                        >(body with { SessionId = sessionId }, ct)
                    )
            )
            .WithName("StartVirtualRoomRecording")
            .WithSummary("Starts a LiveKit room composite MP4 recording egress");

        _ = group
            .MapPost(
                "/sessions/{sessionId:guid}/recordings/stop",
                static async (
                    Guid sessionId,
                    StopVirtualRoomRecordingCommand body,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                    VirtualRoomsHttpResultMapper.ToHttpResult(
                        await commands.SendAsync<
                            StopVirtualRoomRecordingCommand,
                            VirtualRoomRecordingArchiveResponse
                        >(body with { SessionId = sessionId }, ct)
                    )
            )
            .WithName("StopVirtualRoomRecording")
            .WithSummary("Stops an active LiveKit recording egress");
    }

    private static void MapWebhookRoutes(IEndpointRouteBuilder endpoints)
    {
        var webhooks = endpoints
            .MapGroup("/api/virtual-rooms/webhooks")
            .WithTags("Virtual Room Webhooks")
            .AllowAnonymous();

        _ = webhooks
            .MapPost(
                "/livekit",
                static async (
                    HttpContext httpContext,
                    ICommandDispatcher commands,
                    CancellationToken ct
                ) =>
                {
                    using var reader = new StreamReader(
                        httpContext.Request.Body,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true
                    );
                    var rawBody = await reader.ReadToEndAsync(ct);
                    var authHeader = httpContext.Request.Headers.Authorization.ToString();

                    var res = await commands.SendAsync<
                        LiveKitWebhookIngestCommand,
                        LiveKitWebhookIngestResponse
                    >(new LiveKitWebhookIngestCommand(authHeader, rawBody), ct);
                    return VirtualRoomsHttpResultMapper.ToHttpResult(res);
                }
            )
            .WithName("IngestLiveKitWebhook")
            .WithSummary(
                "Verifies and ingests LiveKit webhooks (participant_joined, participant_left, egress_ended)"
            );
    }

    private static SecuritySubject BuildSubjectFromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return SecuritySubject.Anonymous;
        }

        var subClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        Guid? userId = Guid.TryParse(subClaim, out var parsed) ? parsed : null;
        HashSet<Guid> roleIds =
        [
            .. principal
                .FindAll("role_id")
                .Select(static c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
                .Where(static g => g != Guid.Empty),
        ];
        return new SecuritySubject(userId, roleIds);
    }
}
