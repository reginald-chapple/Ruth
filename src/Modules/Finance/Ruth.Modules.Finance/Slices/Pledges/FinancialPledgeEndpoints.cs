using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Pledges;

internal static class FinancialPledgeEndpoints
{
    public static IEndpointRouteBuilder MapFinancialPledgeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/pledges")
            .WithTags("Finance Pledges")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListPledges")
            .WithSummary("List constituent and corporate financial pledges")
            .WithDescription(
                "Returns pledges with live fulfillment amounts, remaining balances, and pro-rata pace."
            )
            .Produces<IReadOnlyList<FinancialPledgeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/analytics", GetAnalyticsAsync)
            .WithName("FinanceGetPledgeAnalytics")
            .WithSummary("Get pledge commitment and fulfillment analytics")
            .WithDescription(
                "Computes total pledged, fulfilled, remaining, and on-track vs. behind-pace pledge counts."
            )
            .Produces<PledgeAnalyticsSummaryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreatePledge")
            .WithSummary("Create a financial pledge")
            .WithDescription(
                "Records a new constituent or corporate pledge commitment toward a financial account."
            )
            .Produces<FinancialPledgeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("FinanceUpdatePledge")
            .WithSummary("Revise an active financial pledge")
            .WithDescription("Updates pledged amount, installment cadence, date range, or notes.")
            .Produces<FinancialPledgeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("FinanceCancelPledge")
            .WithSummary("Cancel an active financial pledge")
            .WithDescription("Cancels an active pledge commitment.")
            .Produces<FinancialPledgeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialPledgeResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? accountId,
        Guid? personId,
        FinancialPledgeStatus? status,
        DateOnly? asOfDate,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialPledgesQuery,
            IReadOnlyList<FinancialPledgeResponse>
        >(new GetFinancialPledgesQuery(accountId, personId, status, asOfDate), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<PledgeAnalyticsSummaryResponse>, ProblemHttpResult>
    > GetAnalyticsAsync(
        Guid? accountId,
        Guid? branchId,
        string? currencyCode,
        DateOnly? asOfDate,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetPledgeAnalyticsQuery,
            PledgeAnalyticsSummaryResponse
        >(
            new GetPledgeAnalyticsQuery(accountId, branchId, currencyCode ?? "USD", asOfDate),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialPledgeResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateFinancialPledgeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateFinancialPledgeCommand,
            FinancialPledgeResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/pledges/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialPledgeResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateFinancialPledgeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateFinancialPledgeCommand,
            FinancialPledgeResponse
        >(command with { PledgeId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialPledgeResponse>, ProblemHttpResult>> CancelAsync(
        Guid id,
        CancelFinancialPledgeCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CancelFinancialPledgeCommand,
            FinancialPledgeResponse
        >(command with { PledgeId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
