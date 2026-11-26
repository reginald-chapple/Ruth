using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal static class SavedPaymentMethodEndpoints
{
    public static IEndpointRouteBuilder MapSavedPaymentMethodEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/payment-methods")
            .WithTags("Finance Saved Payment Methods")
            .RequireAuthorization();

        MapVaultEndpoints(group);
        MapBankIdentifierEndpoints(group);

        return endpoints;
    }

    private static void MapVaultEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListSavedPaymentMethods")
            .WithSummary("List tokenized saved payment methods")
            .WithDescription(
                "Returns vaulted card and ACH payment methods without raw card or account numbers."
            )
            .Produces<IReadOnlyList<SavedPaymentMethodResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", VaultAsync)
            .WithName("FinanceVaultPaymentMethod")
            .WithSummary("Exchange a hosted token and vault a saved payment method")
            .WithDescription(
                "Exchanges a hosted tokenizer single-use token via IPaymentGateway and stores the opaque vault token."
            )
            .Produces<SavedPaymentMethodResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/default", SetDefaultAsync)
            .WithName("FinanceSetDefaultPaymentMethod")
            .WithSummary("Mark a saved payment method as default")
            .WithDescription("Promotes the specified vaulted payment method to default.")
            .Produces<SavedPaymentMethodResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapDelete("/{id:guid}", DeactivateAsync)
            .WithName("FinanceDeactivatePaymentMethod")
            .WithSummary("Deactivate a saved payment method")
            .WithDescription("Deactivates a vaulted payment method.")
            .Produces<SavedPaymentMethodResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapBankIdentifierEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/bank-identifiers", RegisterBankIdentifierAsync)
            .WithName("FinanceRegisterBankIdentifier")
            .WithSummary("Register a hashed routing/account bank identifier")
            .WithDescription(
                "Validates ABA routing checksum and binds the keyed HMAC-SHA256 digest to a constituent for check/ACH matching."
            )
            .Produces<BankAccountIdentifierResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPost("/bank-identifiers/resolve", ResolveBankIdentifierAsync)
            .WithName("FinanceResolveBankIdentifier")
            .WithSummary("Resolve constituent Person from routing and account numbers")
            .WithDescription("Performs O(1) keyed HMAC-SHA256 bank account identifier lookup.")
            .Produces<BankAccountIdentifierResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<SavedPaymentMethodResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? personId,
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetSavedPaymentMethodsQuery,
            IReadOnlyList<SavedPaymentMethodResponse>
        >(new GetSavedPaymentMethodsQuery(personId, activeOnly ?? true), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<SavedPaymentMethodResponse>, ProblemHttpResult>
    > VaultAsync(
        VaultSavedPaymentMethodCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            VaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/payment-methods/{result.Value.Id:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SavedPaymentMethodResponse>, ProblemHttpResult>
    > SetDefaultAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            SetDefaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(new SetDefaultSavedPaymentMethodCommand(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<SavedPaymentMethodResponse>, ProblemHttpResult>
    > DeactivateAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            DeactivateSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(new DeactivateSavedPaymentMethodCommand(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<BankAccountIdentifierResponse>, ProblemHttpResult>
    > RegisterBankIdentifierAsync(
        RegisterBankAccountIdentifierCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RegisterBankAccountIdentifierCommand,
            BankAccountIdentifierResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/payment-methods/bank-identifiers/{result.Value.Id:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<BankAccountIdentifierResponse>, ProblemHttpResult>
    > ResolveBankIdentifierAsync(
        ResolvePersonByBankAccountQuery query,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            ResolvePersonByBankAccountQuery,
            BankAccountIdentifierResponse
        >(query, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
