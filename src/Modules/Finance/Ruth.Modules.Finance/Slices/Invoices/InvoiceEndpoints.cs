using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Invoices;

internal static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/invoices")
            .WithTags("Finance Invoices")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListInvoices")
            .WithSummary("List membership dues, fee, rental, and sponsorship invoices")
            .WithDescription("Returns invoices and balance-due projections.")
            .Produces<IReadOnlyList<InvoiceResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapGet("/{id:guid}", GetByIdAsync)
            .WithName("FinanceGetInvoiceById")
            .WithSummary("Get invoice by ID")
            .WithDescription("Returns full invoice details and line items.")
            .Produces<InvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateInvoice")
            .WithSummary("Create a new invoice")
            .WithDescription("Creates a Draft or Open invoice with line items.")
            .Produces<InvoiceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapPost("/{id:guid}/issue", IssueAsync)
            .WithName("FinanceIssueInvoice")
            .WithSummary("Issue a draft invoice")
            .WithDescription("Transitions a Draft invoice to Open.")
            .Produces<InvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/void", VoidAsync)
            .WithName("FinanceVoidInvoice")
            .WithSummary("Void an unpaid invoice")
            .WithDescription("Voids an invoice that has zero active settled payments.")
            .Produces<InvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<InvoiceResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? personId,
        InvoiceStatus? status,
        Guid? branchId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetInvoicesQuery, IReadOnlyList<InvoiceResponse>>(
            new GetInvoicesQuery(personId, status, branchId),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
            new GetInvoiceByIdQuery(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Created<InvoiceResponse>, ProblemHttpResult>> CreateAsync(
        CreateInvoiceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            command,
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/invoices/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> IssueAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<IssueInvoiceCommand, InvoiceResponse>(
            new IssueInvoiceCommand(id),
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<InvoiceResponse>, ProblemHttpResult>> VoidAsync(
        Guid id,
        VoidInvoiceCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<VoidInvoiceCommand, InvoiceResponse>(
            command with
            {
                InvoiceId = id,
            },
            cancellationToken
        );
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
