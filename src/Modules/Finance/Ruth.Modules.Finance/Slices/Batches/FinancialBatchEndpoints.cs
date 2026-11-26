using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Batches;

internal static class FinancialBatchEndpoints
{
    public static IEndpointRouteBuilder MapFinancialBatchEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/batches")
            .WithTags("Finance Batches")
            .RequireAuthorization();

        MapQueryAndCreateEndpoints(group);
        MapLifecycleEndpoints(group);

        return endpoints;
    }

    private static void MapQueryAndCreateEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListBatches")
            .WithSummary("List financial settlement batches")
            .WithDescription(
                "Returns financial batches with control vs. actual variance and balance state."
            )
            .Produces<IReadOnlyList<FinancialBatchResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("FinanceGetBatchById")
            .WithSummary("Get financial batch by ID")
            .WithDescription("Returns reconciliation details for a single financial batch.")
            .Produces<FinancialBatchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateBatch")
            .WithSummary("Create a financial settlement batch")
            .WithDescription("Creates a Pending or Open settlement batch.")
            .Produces<FinancialBatchResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static void MapLifecycleEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/open", OpenAsync)
            .WithName("FinanceOpenBatch")
            .WithSummary("Open a Pending financial batch")
            .WithDescription("Transitions a Pending batch to Open or updates control targets.")
            .Produces<FinancialBatchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/close", CloseAsync)
            .WithName("FinanceCloseBatch")
            .WithSummary("Reconcile and close an Open financial batch")
            .WithDescription(
                "Verifies zero control amount/count variance and audit-locks the batch."
            )
            .Produces<FinancialBatchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/reopen", ReopenAsync)
            .WithName("FinanceReopenBatch")
            .WithSummary("Explicitly reopen an audit-locked Closed batch")
            .WithDescription("Reopens a Closed batch and logs the audit reason.")
            .Produces<FinancialBatchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialBatchResponse>>, ProblemHttpResult>
    > ListAsync(
        FinancialBatchStatus? status,
        Guid? branchId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialBatchesQuery,
            IReadOnlyList<FinancialBatchResponse>
        >(new GetFinancialBatchesQuery(status, branchId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialBatchResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialBatchByIdQuery,
            FinancialBatchResponse
        >(new GetFinancialBatchByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialBatchResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateFinancialBatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateFinancialBatchCommand,
            FinancialBatchResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/batches/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialBatchResponse>, ProblemHttpResult>> OpenAsync(
        Guid id,
        OpenFinancialBatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<OpenFinancialBatchCommand, FinancialBatchResponse>(
            command with
            {
                BatchId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialBatchResponse>, ProblemHttpResult>> CloseAsync(
        Guid id,
        CloseFinancialBatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CloseFinancialBatchCommand, FinancialBatchResponse>(
            command with
            {
                BatchId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialBatchResponse>, ProblemHttpResult>> ReopenAsync(
        Guid id,
        ReopenFinancialBatchCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ReopenFinancialBatchCommand,
            FinancialBatchResponse
        >(command with { BatchId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
