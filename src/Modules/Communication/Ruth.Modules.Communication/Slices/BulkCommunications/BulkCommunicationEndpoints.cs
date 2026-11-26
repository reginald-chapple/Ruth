using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal static class BulkCommunicationEndpoints
{
    public static IEndpointRouteBuilder MapBulkCommunicationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/campaigns")
            .WithTags("Communication Bulk Campaigns")
            .RequireAuthorization();

        RegisterCrudRoutes(group);
        RegisterLifecycleRoutes(group);
        return endpoints;
    }

    private static void RegisterCrudRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListCampaigns")
            .WithSummary("List bulk and targeted communication campaigns")
            .Produces<IReadOnlyList<CommunicationSummaryResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CommunicationGetCampaignById")
            .WithSummary("Get full campaign detail and recipient telemetry")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CommunicationCreateCampaign")
            .WithSummary("Create a new draft communication campaign")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateDraftAsync)
            .WithName("CommunicationUpdateCampaignDraft")
            .WithSummary("Update an editable draft or rejected communication campaign")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static void RegisterLifecycleRoutes(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/submit", SubmitAsync)
            .WithName("CommunicationSubmitCampaign")
            .WithSummary("Submit a draft communication for approval")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/approve", ApproveAsync)
            .WithName("CommunicationApproveCampaign")
            .WithSummary("Approve a communication campaign for batch dispatch")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/reject", RejectAsync)
            .WithName("CommunicationRejectCampaign")
            .WithSummary("Reject a pending communication campaign with reviewer feedback")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("CommunicationCancelCampaign")
            .WithSummary("Cancel a communication campaign before completion")
            .Produces<CommunicationDetailResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/dispatch-due", DispatchDueAsync)
            .WithName("CommunicationDispatchDueCampaigns")
            .WithSummary("Execute an immediate batch dispatch pass for due approved campaigns")
            .Produces<BulkDispatchBatchResultResponse>(StatusCodes.Status200OK);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommunicationSummaryResponse>>, ProblemHttpResult>
    > ListAsync(
        CommunicationStatus? status,
        CommunicationChannel? channel,
        int? limit,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationsQuery,
            IReadOnlyList<CommunicationSummaryResponse>
        >(new GetCommunicationsQuery(status, channel, limit ?? 50), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationByIdQuery,
            CommunicationDetailResponse
        >(new GetCommunicationByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CommunicationDetailResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateCommunicationCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateCommunicationCommand,
            CommunicationDetailResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/campaigns/{result.Value.Id:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > UpdateDraftAsync(
        Guid id,
        UpdateCommunicationDraftCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCommunicationDraftCommand,
            CommunicationDetailResponse
        >(command with { CommunicationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > SubmitAsync(
        Guid id,
        SubmitCommunicationForApprovalCommand? body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var cmd = (body ?? new SubmitCommunicationForApprovalCommand(id)) with
        {
            CommunicationId = id,
        };
        var result = await dispatcher.SendAsync<
            SubmitCommunicationForApprovalCommand,
            CommunicationDetailResponse
        >(cmd, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > ApproveAsync(
        Guid id,
        ApproveCommunicationCommand? body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var cmd = (body ?? new ApproveCommunicationCommand(id)) with { CommunicationId = id };
        var result = await dispatcher.SendAsync<
            ApproveCommunicationCommand,
            CommunicationDetailResponse
        >(cmd, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > RejectAsync(
        Guid id,
        RejectCommunicationCommand body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RejectCommunicationCommand,
            CommunicationDetailResponse
        >(body with { CommunicationId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationDetailResponse>, ProblemHttpResult>
    > CancelAsync(
        Guid id,
        CancelCommunicationCommand? body,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var cmd = (body ?? new CancelCommunicationCommand(id)) with { CommunicationId = id };
        var result = await dispatcher.SendAsync<
            CancelCommunicationCommand,
            CommunicationDetailResponse
        >(cmd, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<BulkDispatchBatchResultResponse>, ProblemHttpResult>
    > DispatchDueAsync(
        ProcessDueBulkCommunicationsCommand? command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ProcessDueBulkCommunicationsCommand,
            BulkDispatchBatchResultResponse
        >(command ?? new ProcessDueBulkCommunicationsCommand(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
