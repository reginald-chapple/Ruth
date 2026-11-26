using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal static class InteractiveSessionEndpoints
{
    public static IEndpointRouteBuilder MapInteractiveSessionEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var moderatorGroup = endpoints
            .MapGroup("/api/events/interactive-sessions")
            .WithTags("Events Interactive Sessions")
            .RequireAuthorization();

        MapModeratorLifecycleRoutes(moderatorGroup);
        MapModeratorControlRoutes(moderatorGroup);

        var publicGroup = endpoints
            .MapGroup("/api/events/interactive-sessions/live")
            .WithTags("Events Interactive Sessions")
            .AllowAnonymous();

        MapAudienceLiveRoutes(publicGroup);
        return endpoints;
    }

    private static void MapModeratorLifecycleRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListSessionsAsync)
            .WithName("EventsListInteractiveSessions")
            .WithSummary("List interactive sessions")
            .WithDescription("Returns town-hall, conference, and classroom interactive sessions.")
            .Produces<IReadOnlyList<InteractiveSessionDetailResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateSessionAsync)
            .WithName("EventsCreateInteractiveSession")
            .WithSummary("Create an interactive session")
            .WithDescription(
                "Creates a live interactive session with a unique 6-character join code."
            )
            .Produces<InteractiveSessionDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPost("/{id:guid}/occurrences/start", StartOccurrenceAsync)
            .WithName("EventsStartInteractiveSessionOccurrence")
            .WithSummary("Start a live interactive session occurrence")
            .WithDescription("Starts a live occurrence and broadcasts stage state over SignalR.")
            .Produces<InteractiveSessionDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/end", EndSessionAsync)
            .WithName("EventsEndInteractiveSession")
            .WithSummary("End an interactive session")
            .WithDescription(
                "Concludes the live session and updates connected SignalR stage views."
            )
            .Produces<InteractiveSessionDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapModeratorControlRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/actions", AddActionAsync)
            .WithName("EventsAddInteractiveAction")
            .WithSummary("Add a poll, Q&A prompt, or slide action to a session")
            .WithDescription("Appends an interactive action to the session.")
            .Produces<InteractiveActionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}/active-action", SetActiveActionAsync)
            .WithName("EventsSetActiveInteractiveAction")
            .WithSummary("Project a specific action onto the live stage")
            .WithDescription("Switches the active stage prompt and pushes via SignalR.")
            .Produces<InteractiveStageProjectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/responses/{responseId:guid}/moderate", ModerateResponseAsync)
            .WithName("EventsModerateInteractiveResponse")
            .WithSummary("Approve, hold, hide, or upvote an audience response")
            .WithDescription("Updates audience response moderation state and refreshes stage.")
            .Produces<InteractiveStageProjectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapAudienceLiveRoutes(RouteGroupBuilder publicGroup)
    {
        _ = publicGroup
            .MapGet("/{codeOrId}", GetLiveStageAsync)
            .WithName("EventsGetInteractiveStageProjection")
            .WithSummary("Get live stage projection by session ID or 6-character join code")
            .WithDescription(
                "Returns active prompt, poll tallies, and approved Q&A for audience/stage clients."
            )
            .Produces<InteractiveStageProjectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = publicGroup
            .MapPost("/{codeOrId}/responses", SubmitAudienceResponseAsync)
            .WithName("EventsSubmitInteractiveResponse")
            .WithSummary("Submit an audience poll vote or Q&A response")
            .WithDescription("Records a participant response and broadcasts live updates.")
            .Produces<InteractiveStageProjectionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<InteractiveSessionDetailResponse>>, ProblemHttpResult>
    > ListSessionsAsync(
        InteractiveSessionStatus? status,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetInteractiveSessionsQuery,
            IReadOnlyList<InteractiveSessionDetailResponse>
        >(new GetInteractiveSessionsQuery(status), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<InteractiveSessionDetailResponse>, ProblemHttpResult>
    > CreateSessionAsync(
        CreateInteractiveSessionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            CreateInteractiveSessionCommand,
            InteractiveSessionDetailResponse
        >(command, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/events/interactive-sessions/{res.Value.Id:D}", res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<InteractiveSessionDetailResponse>, ProblemHttpResult>
    > StartOccurrenceAsync(
        Guid id,
        StartInteractiveSessionOccurrenceCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            StartInteractiveSessionOccurrenceCommand,
            InteractiveSessionDetailResponse
        >(body with { SessionId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<InteractiveSessionDetailResponse>, ProblemHttpResult>
    > EndSessionAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            EndInteractiveSessionCommand,
            InteractiveSessionDetailResponse
        >(new EndInteractiveSessionCommand(id), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<InteractiveActionResponse>, ProblemHttpResult>
    > AddActionAsync(
        Guid id,
        InteractiveActionSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            AddInteractiveActionCommand,
            InteractiveActionResponse
        >(new AddInteractiveActionCommand(id, spec), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created(
                $"/api/events/interactive-sessions/{id:D}/actions/{res.Value.Id:D}",
                res.Value
            )
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<InteractiveStageProjectionResponse>, ProblemHttpResult>
    > SetActiveActionAsync(
        Guid id,
        SetActiveInteractiveActionCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            SetActiveInteractiveActionCommand,
            InteractiveStageProjectionResponse
        >(body with { SessionId = id }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<InteractiveStageProjectionResponse>, ProblemHttpResult>
    > ModerateResponseAsync(
        Guid id,
        Guid responseId,
        ModerateInteractiveResponseCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            ModerateInteractiveResponseCommand,
            InteractiveStageProjectionResponse
        >(body with { SessionId = id, ResponseId = responseId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Ok<InteractiveStageProjectionResponse>, ProblemHttpResult>
    > GetLiveStageAsync(
        string codeOrId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.QueryAsync<
            GetInteractiveSessionStageQuery,
            InteractiveStageProjectionResponse
        >(new GetInteractiveSessionStageQuery(codeOrId), cancellationToken);
        return res.IsSuccess
            ? TypedResults.Ok(res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }

    private static async Task<
        Results<Created<InteractiveStageProjectionResponse>, ProblemHttpResult>
    > SubmitAudienceResponseAsync(
        string codeOrId,
        SubmitInteractiveResponseCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var res = await dispatcher.SendAsync<
            SubmitInteractiveResponseCommand,
            InteractiveStageProjectionResponse
        >(body with { SessionIdOrJoinCode = codeOrId }, cancellationToken);
        return res.IsSuccess
            ? TypedResults.Created($"/api/events/interactive-sessions/live/{codeOrId}", res.Value)
            : EventsProblemResultMapper.ToProblemResult(res, httpContext);
    }
}
