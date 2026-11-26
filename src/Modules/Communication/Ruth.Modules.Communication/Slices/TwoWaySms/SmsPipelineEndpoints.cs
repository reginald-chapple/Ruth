using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal static class SmsPipelineEndpoints
{
    public static IEndpointRouteBuilder MapSmsPipelineEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/communication/sms/pipelines")
            .WithTags("Communication Two-Way SMS & Keyword Pipelines")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListPipelinesAsync)
            .WithName("CommunicationListSmsPipelines")
            .WithSummary("List inbound SMS keyword responder pipelines")
            .Produces<IReadOnlyList<SmsPipelineResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreatePipelineAsync)
            .WithName("CommunicationCreateSmsPipeline")
            .WithSummary("Create an SMS keyword responder pipeline")
            .Produces<SmsPipelineResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdatePipelineAsync)
            .WithName("CommunicationUpdateSmsPipeline")
            .WithSummary("Update an SMS keyword responder pipeline")
            .Produces<SmsPipelineResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeletePipelineAsync)
            .WithName("CommunicationDeleteSmsPipeline")
            .WithSummary("Delete a non-system SMS keyword responder pipeline")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SmsPipelineResponse>>, ProblemHttpResult>
    > ListPipelinesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSmsPipelinesQuery,
            IReadOnlyList<SmsPipelineResponse>
        >(new GetSmsPipelinesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SmsPipelineResponse>, ProblemHttpResult>
    > CreatePipelineAsync(
        CreateSmsPipelineCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateSmsPipelineCommand, SmsPipelineResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/communication/sms/pipelines/{result.Value.Id:D}",
                result.Value
            )
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SmsPipelineResponse>, ProblemHttpResult>
    > UpdatePipelineAsync(
        Guid id,
        UpdateSmsPipelineCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<UpdateSmsPipelineCommand, SmsPipelineResponse>(
            command with
            {
                PipelineId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeletePipelineAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<DeleteSmsPipelineCommand, bool>(
            new DeleteSmsPipelineCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.NoContent()
            : CommunicationProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
