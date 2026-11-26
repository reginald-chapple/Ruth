using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal static class FinancialAccountEndpoints
{
    public static IEndpointRouteBuilder MapFinancialAccountEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/accounts")
            .WithTags("Finance Accounts")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListAccounts")
            .WithSummary("List hierarchical financial accounts and funds")
            .WithDescription(
                "Returns financial accounts filtered optionally by kind, branch, tax deductibility, and active status."
            )
            .Produces<IReadOnlyList<FinancialAccountResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("FinanceGetAccountById")
            .WithSummary("Get financial account by ID")
            .WithDescription(
                "Returns full configuration details for a financial account or charitable fund."
            )
            .Produces<FinancialAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateAccount")
            .WithSummary("Create a financial account or fund")
            .WithDescription(
                "Creates a hierarchical financial account supporting charitable funds, membership dues, fees, grants, or operating revenue."
            )
            .Produces<FinancialAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("FinanceUpdateAccount")
            .WithSummary("Update a financial account or fund")
            .WithDescription(
                "Updates an existing financial account while enforcing hierarchy cycle and date window invariants."
            )
            .Produces<FinancialAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<FinancialAccountResponse>>, ProblemHttpResult>
    > ListAsync(
        FinancialAccountKind? accountKind,
        Guid? branchId,
        bool? isTaxDeductible,
        bool? activeOnly,
        bool? publicOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(
            new GetFinancialAccountsQuery(
                accountKind,
                branchId,
                isTaxDeductible,
                activeOnly ?? false,
                publicOnly ?? false
            ),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<FinancialAccountResponse>, ProblemHttpResult>
    > GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse
        >(new GetFinancialAccountByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<FinancialAccountResponse>, ProblemHttpResult>
    > CreateAsync(
        FinancialAccountSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(new CreateFinancialAccountCommand(spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/accounts/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<FinancialAccountResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        FinancialAccountSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateFinancialAccountCommand,
            FinancialAccountResponse
        >(new UpdateFinancialAccountCommand(id, spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
