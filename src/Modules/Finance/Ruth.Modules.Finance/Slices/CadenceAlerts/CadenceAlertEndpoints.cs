using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal static class CadenceAlertEndpoints
{
    public static IEndpointRouteBuilder MapCadenceAlertEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/cadence-alerts")
            .WithTags("Finance Cadence Alerts")
            .RequireAuthorization();

        _ = group
            .MapGet("/", ListAlertsAsync)
            .WithName("FinanceListCadenceAlerts")
            .WithSummary("List detected lapse/churn risk and unusual spike alerts")
            .WithDescription(
                "Returns transaction cadence alerts filtered by status, kind, or person."
            )
            .Produces<IReadOnlyList<TransactionCadenceAlertResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/scan", ScanAsync)
            .WithName("FinanceRunCadenceScan")
            .WithSummary("Run statistical cadence and spike scan")
            .WithDescription(
                "Evaluates constituent historical transaction cadence and recurring past-due state."
            )
            .Produces<CadenceScanSummaryResponse>(StatusCodes.Status200OK);

        _ = group
            .MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .WithName("FinanceUpdateCadenceAlertStatus")
            .WithSummary("Acknowledge, resolve, or dismiss a cadence alert")
            .WithDescription("Updates review status and resolution notes on a cadence alert.")
            .Produces<TransactionCadenceAlertResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        _ = group
            .MapGet("/types", ListTypesAsync)
            .WithName("FinanceListCadenceAlertTypes")
            .WithSummary("List cadence and spike alert rule definitions")
            .WithDescription("Returns configured statistical threshold rules.")
            .Produces<IReadOnlyList<TransactionCadenceAlertTypeResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/types", CreateTypeAsync)
            .WithName("FinanceCreateCadenceAlertType")
            .WithSummary("Create a cadence or spike alert rule")
            .WithDescription("Creates a new statistical cadence or spike alert threshold rule.")
            .Produces<TransactionCadenceAlertTypeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        _ = group
            .MapPut("/types/{id:guid}", UpdateTypeAsync)
            .WithName("FinanceUpdateCadenceAlertType")
            .WithSummary("Update a cadence or spike alert rule")
            .WithDescription("Updates thresholds on an existing cadence alert rule.")
            .Produces<TransactionCadenceAlertTypeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TransactionCadenceAlertResponse>>, ProblemHttpResult>
    > ListAlertsAsync(
        CadenceAlertStatus? status,
        CadenceAlertKind? alertKind,
        Guid? personId,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetTransactionCadenceAlertsQuery,
            IReadOnlyList<TransactionCadenceAlertResponse>
        >(new GetTransactionCadenceAlertsQuery(status, alertKind, personId), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<CadenceScanSummaryResponse>, ProblemHttpResult>> ScanAsync(
        RunTransactionCadenceScanCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            RunTransactionCadenceScanCommand,
            CadenceScanSummaryResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TransactionCadenceAlertResponse>, ProblemHttpResult>
    > UpdateStatusAsync(
        Guid id,
        UpdateCadenceAlertStatusCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCadenceAlertStatusCommand,
            TransactionCadenceAlertResponse
        >(command with { AlertId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<TransactionCadenceAlertTypeResponse>>, ProblemHttpResult>
    > ListTypesAsync(
        bool? activeOnly,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetCadenceAlertTypesQuery,
            IReadOnlyList<TransactionCadenceAlertTypeResponse>
        >(new GetCadenceAlertTypesQuery(activeOnly ?? false), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<TransactionCadenceAlertTypeResponse>, ProblemHttpResult>
    > CreateTypeAsync(
        TransactionCadenceAlertTypeSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateCadenceAlertTypeCommand,
            TransactionCadenceAlertTypeResponse
        >(new CreateCadenceAlertTypeCommand(spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created(
                $"/api/finance/cadence-alerts/types/{result.Value.Id:D}",
                result.Value
            )
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<TransactionCadenceAlertTypeResponse>, ProblemHttpResult>
    > UpdateTypeAsync(
        Guid id,
        TransactionCadenceAlertTypeSpec spec,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateCadenceAlertTypeCommand,
            TransactionCadenceAlertTypeResponse
        >(new UpdateCadenceAlertTypeCommand(id, spec), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
