using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Pipelines;

internal static class EngagementConnectorBoardEndpoints
{
    public static void MapRequestAndBoardRoutes(RouteGroupBuilder group)
    {
        MapBoardAndIntakeRoutes(group);
        MapLifecycleActionRoutes(group);
    }

    private static void MapBoardAndIntakeRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/{pipelineTypeId:guid}/board", GetConnectorBoardAsync)
            .WithName("EngagementGetConnectorBoard")
            .WithSummary("Load Kanban and Grid Connector Board")
            .WithDescription(
                "Returns Kanban stage columns, SLA health metrics, and flat grid items."
            )
            .Produces<ConnectorBoardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/requests", ListRequestsAsync)
            .WithName("EngagementListRequests")
            .WithSummary("Query engagement requests")
            .WithDescription(
                "Filters engagement requests by pipeline, opportunity, requester, or state."
            )
            .Produces<IReadOnlyList<EngagementRequestResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/requests", CreateRequestAsync)
            .WithName("EngagementCreateRequest")
            .WithSummary("Create staff engagement request")
            .WithDescription("Creates an engagement request for an existing CRM constituent.")
            .Produces<EngagementRequestResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static void MapLifecycleActionRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/requests/{id:guid}/transition", TransitionStageAsync)
            .WithName("EngagementTransitionRequestStage")
            .WithSummary("Transition engagement request stage")
            .WithDescription(
                "Moves a request to a target stage and runs declarative stage automations."
            )
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/requests/{id:guid}/activities", RecordActivityAsync)
            .WithName("EngagementRecordRequestActivity")
            .WithSummary("Log coordinator activity on engagement request")
            .WithDescription("Appends an activity log entry and resets idle SLA timers.")
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/requests/{id:guid}/assign", AssignConnectorAsync)
            .WithName("EngagementAssignConnector")
            .WithSummary("Assign coordinator to engagement request")
            .WithDescription(
                "Assigns or reassigns the coordinator responsible for an engagement request."
            )
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/requests/{id:guid}/follow-up", ScheduleFollowUpAsync)
            .WithName("EngagementScheduleFollowUp")
            .WithSummary("Schedule future follow-up")
            .WithDescription(
                "Places an engagement request into FutureFollowUp until the scheduled date."
            )
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/requests/{id:guid}/connect", ConnectRequestAsync)
            .WithName("EngagementConnectRequest")
            .WithSummary("Mark engagement request Connected")
            .WithDescription(
                "Marks the request Connected and registers placement group membership."
            )
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/requests/{id:guid}/inactive", MarkInactiveAsync)
            .WithName("EngagementMarkRequestInactive")
            .WithSummary("Mark engagement request Inactive")
            .WithDescription("Closes an engagement request without connection.")
            .Produces<EngagementRequestResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<ConnectorBoardResponse>, ProblemHttpResult>
    > GetConnectorBoardAsync(
        Guid pipelineTypeId,
        Guid? opportunityId,
        Guid? branchId,
        Guid? connectorPersonId,
        bool? includeClosed,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<GetConnectorBoardQuery, ConnectorBoardResponse>(
            new GetConnectorBoardQuery(
                pipelineTypeId,
                opportunityId,
                branchId,
                connectorPersonId,
                includeClosed ?? false
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<EngagementRequestResponse>>, ProblemHttpResult>
    > ListRequestsAsync(
        Guid? pipelineTypeId,
        Guid? opportunityId,
        Guid? requesterPersonId,
        Guid? connectorPersonId,
        EngagementRequestState? state,
        IQueryDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.QueryAsync<
            GetEngagementRequestsQuery,
            IReadOnlyList<EngagementRequestResponse>
        >(
            new GetEngagementRequestsQuery(
                pipelineTypeId,
                opportunityId,
                requesterPersonId,
                connectorPersonId,
                state
            ),
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Created<EngagementRequestResponse>, ProblemHttpResult>
    > CreateRequestAsync(
        CreateEngagementRequestCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<CreateEngagementRequestCommand, EngagementRequestResponse>(
            body,
            ct
        );
        return r.IsSuccess
            ? TypedResults.Created($"/api/engagement/pipelines/requests/{r.Value.Id:D}", r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > TransitionStageAsync(
        Guid id,
        TransitionEngagementRequestStageCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<
            TransitionEngagementRequestStageCommand,
            EngagementRequestResponse
        >(body with { RequestId = id }, ct);
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > RecordActivityAsync(
        Guid id,
        RecordEngagementActivityCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<RecordEngagementActivityCommand, EngagementRequestResponse>(
            body with
            {
                RequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > AssignConnectorAsync(
        Guid id,
        AssignEngagementConnectorCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<AssignEngagementConnectorCommand, EngagementRequestResponse>(
            body with
            {
                RequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > ScheduleFollowUpAsync(
        Guid id,
        ScheduleEngagementFollowUpCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ScheduleEngagementFollowUpCommand, EngagementRequestResponse>(
            body with
            {
                RequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > ConnectRequestAsync(
        Guid id,
        ConnectEngagementRequestCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<ConnectEngagementRequestCommand, EngagementRequestResponse>(
            body with
            {
                RequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }

    private static async Task<
        Results<Ok<EngagementRequestResponse>, ProblemHttpResult>
    > MarkInactiveAsync(
        Guid id,
        MarkEngagementRequestInactiveCommand body,
        ICommandDispatcher d,
        HttpContext ctx,
        CancellationToken ct
    )
    {
        var r = await d.SendAsync<MarkEngagementRequestInactiveCommand, EngagementRequestResponse>(
            body with
            {
                RequestId = id,
            },
            ct
        );
        return r.IsSuccess
            ? TypedResults.Ok(r.Value)
            : EngagementProblemResultMapper.ToProblemResult(r, ctx);
    }
}
