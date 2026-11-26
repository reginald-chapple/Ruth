using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal static class FinancialGatewayEndpoints
{
    public static IEndpointRouteBuilder MapFinancialGatewayEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/gateways")
            .WithTags("Finance Gateways")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListGateways")
            .WithSummary("List configured financial payment gateways")
            .WithDescription("Returns configured card/ACH payment gateways and provider readiness.")
            .Produces<IReadOnlyList<FinancialGatewayResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/providers", ListProvidersAsync)
            .WithName("FinanceListGatewayProviders")
            .WithSummary("List registered IPaymentGateway providers")
            .WithDescription("Returns compiled payment gateway adapters registered in DI.")
            .Produces<IReadOnlyList<RegisteredPaymentGatewayProviderResponse>>(
                StatusCodes.Status200OK
            );

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateGateway")
            .WithSummary("Create a financial payment gateway configuration")
            .WithDescription("Creates a new gateway profile bound to a payment gateway provider.")
            .Produces<FinancialGatewayResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("FinanceUpdateGateway")
            .WithSummary("Update a financial payment gateway configuration")
            .WithDescription("Updates settings, active state, or capabilities on a gateway.")
            .Produces<FinancialGatewayResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialGatewayResponse>>, ProblemHttpResult>
    > ListAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RegisteredPaymentGatewayProviderResponse>>, ProblemHttpResult>
    > ListProvidersAsync(
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetRegisteredPaymentGatewayProvidersQuery,
            IReadOnlyList<RegisteredPaymentGatewayProviderResponse>
        >(new GetRegisteredPaymentGatewayProvidersQuery(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialGatewayResponse>, ProblemHttpResult>
    > CreateAsync(
        FinancialGatewaySpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateFinancialGatewayCommand,
            FinancialGatewayResponse
        >(new CreateFinancialGatewayCommand(spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/gateways/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialGatewayResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        FinancialGatewaySpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateFinancialGatewayCommand,
            FinancialGatewayResponse
        >(new UpdateFinancialGatewayCommand(id, spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
