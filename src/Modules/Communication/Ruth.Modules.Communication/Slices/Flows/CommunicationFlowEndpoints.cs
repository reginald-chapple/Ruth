using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Flows;

internal static class CommunicationFlowEndpoints
{
    public static IEndpointRouteBuilder MapCommunicationFlowEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/flows")
            .WithTags("Communication Drip Flows & Analytics")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("CommunicationListFlows")
            .WithSummary("List automated multi-step communication flows")
            .Produces<IReadOnlyList<CommunicationFlowResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("CommunicationGetFlowById")
            .WithSummary("Get a communication flow definition by identifier")
            .Produces<CommunicationFlowResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("CommunicationCreateFlow")
            .WithSummary("Create a multi-step communication drip flow")
            .Produces<CommunicationFlowResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("CommunicationUpdateFlow")
            .WithSummary("Update an existing communication drip flow")
            .Produces<CommunicationFlowResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeleteAsync)
            .WithName("CommunicationDeleteFlow")
            .WithSummary("Delete a non-system communication drip flow")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = endpoints.MapCommunicationFlowExecutionEndpoints();
        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<CommunicationFlowResponse>>, ProblemHttpResult>
    > ListAsync(
        CommunicationFlowCategory? category,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationFlowsQuery,
            IReadOnlyList<CommunicationFlowResponse>
        >(new GetCommunicationFlowsQuery(category, activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationFlowResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCommunicationFlowByIdQuery,
            CommunicationFlowResponse
        >(new GetCommunicationFlowByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<CommunicationFlowResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateCommunicationFlowCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateCommunicationFlowCommand,
            CommunicationFlowResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/communication/flows/{result.Value.Id:D}", result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<CommunicationFlowResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateCommunicationFlowCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCommunicationFlowCommand,
            CommunicationFlowResponse
        >(command with { FlowId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteCommunicationFlowCommand, bool>(
            new DeleteCommunicationFlowCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
