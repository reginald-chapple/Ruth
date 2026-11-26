using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Flows;

internal static class CommunicationFlowExecutionEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationFlowExecutionEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/flows")
            .WithTags("Communication Drip Flows & Analytics")
            .RequireAuthorization();

        _ = group
            .MapGet("/{id:guid}/instances", ListInstancesAsync)
            .WithName("CommunicationListFlowInstances")
            .WithSummary("List cohort instances and enrolled recipients for a flow")
            .Produces<IReadOnlyList<CommunicationFlowInstanceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}/analytics", GetAnalyticsAsync)
            .WithName("CommunicationGetFlowAnalytics")
            .WithSummary("Get funnel, engagement, and conversion analytics for a flow")
            .Produces<CommunicationFlowAnalyticsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/enroll", EnrollAsync)
            .WithName("CommunicationEnrollFlowRecipients")
            .WithSummary("Enroll recipients into a communication drip flow instance")
            .Produces<CommunicationFlowInstanceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/conversions", RecordConversionAsync)
            .WithName("CommunicationRecordFlowConversion")
            .WithSummary("Record a conversion or engagement signal for an enrolled flow recipient")
            .Produces<CommunicationFlowConversionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/advance-due", AdvanceDueAsync)
            .WithName("CommunicationAdvanceDueFlows")
            .WithSummary("Execute an immediate drip advancement pass for due flow steps")
            .Produces<FlowAdvancementBatchResultResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommunicationFlowInstanceResponse>>, ProblemHttpResult>
    > ListInstancesAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationFlowInstancesQuery,
            IReadOnlyList<CommunicationFlowInstanceResponse>
        >(new GetCommunicationFlowInstancesQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationFlowAnalyticsResponse>, ProblemHttpResult>
    > GetAnalyticsAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationFlowAnalyticsQuery,
            CommunicationFlowAnalyticsResponse
        >(new GetCommunicationFlowAnalyticsQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CommunicationFlowInstanceResponse>, ProblemHttpResult>
    > EnrollAsync(
        Guid id,
        EnrollCommunicationFlowRecipientsCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            EnrollCommunicationFlowRecipientsCommand,
            CommunicationFlowInstanceResponse
        >(command with { FlowId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/communication/flows/{id:D}/instances", result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CommunicationFlowConversionResponse>, ProblemHttpResult>
    > RecordConversionAsync(
        RecordFlowConversionCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RecordFlowConversionCommand,
            CommunicationFlowConversionResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/flows/{result.Value.FlowId:D}/analytics",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<FlowAdvancementBatchResultResponse>, ProblemHttpResult>
    > AdvanceDueAsync(
        ProcessDueCommunicationFlowsCommand? command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ProcessDueCommunicationFlowsCommand,
            FlowAdvancementBatchResultResponse
        >(command ?? new ProcessDueCommunicationFlowsCommand(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
