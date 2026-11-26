using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal static class RecurringScheduleEndpoints
{
    public static IEndpointRouteBuilder MapRecurringScheduleEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/finance/recurring")
            .WithTags("Finance Recurring Schedules")
            .RequireAuthorization();

        MapCrudEndpoints(group);
        MapLifecycleAndSyncEndpoints(group);
        MapGatewayWebhookEndpoint(endpoints);

        return endpoints;
    }

    private static void MapCrudEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapGet("/", ListAsync)
            .WithName("FinanceListRecurringSchedules")
            .WithSummary("List recurring contribution and membership subscription schedules")
            .WithDescription("Returns recurring schedules with splits and next billing dates.")
            .Produces<IReadOnlyList<RecurringScheduleResponse>>(StatusCodes.Status200OK);

        _ = group
            .MapPost("/", CreateAsync)
            .WithName("FinanceCreateRecurringSchedule")
            .WithSummary("Create a recurring gift or membership subscription schedule")
            .WithDescription("Creates a recurring schedule and synchronizes it with the gateway.")
            .Produces<RecurringScheduleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPut("/{id:guid}", UpdateAsync)
            .WithName("FinanceUpdateRecurringSchedule")
            .WithSummary("Update recurring schedule splits, cadence, or payment method")
            .WithDescription(
                "Self-service or staff update for recurring contribution and membership schedules."
            )
            .Produces<RecurringScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapLifecycleAndSyncEndpoints(RouteGroupBuilder group)
    {
        _ = group
            .MapPost("/{id:guid}/pause", PauseAsync)
            .WithName("FinancePauseRecurringSchedule")
            .WithSummary("Pause an active recurring schedule")
            .WithDescription("Pauses future automated settlements until resumed.")
            .Produces<RecurringScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/resume", ResumeAsync)
            .WithName("FinanceResumeRecurringSchedule")
            .WithSummary("Resume a paused or past-due recurring schedule")
            .WithDescription("Reactivates a Paused or PastDue schedule.")
            .Produces<RecurringScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/{id:guid}/cancel", CancelAsync)
            .WithName("FinanceCancelRecurringSchedule")
            .WithSummary("Cancel a recurring schedule")
            .WithDescription("Permanently cancels a recurring schedule.")
            .Produces<RecurringScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        _ = group
            .MapPost("/process-due", ProcessDueAsync)
            .WithName("FinanceProcessDueRecurringSchedules")
            .WithSummary("Execute settlement sync for due recurring schedules")
            .WithDescription(
                "Charges due active recurring schedules and advances next payment dates."
            )
            .Produces<RecurringSettlementRunResponse>(StatusCodes.Status200OK);
    }

    private static void MapGatewayWebhookEndpoint(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints
            .MapPost("/api/finance/gateways/{gatewayId:guid}/webhooks", ReceiveWebhookAsync)
            .AllowAnonymous()
            .WithTags("Finance Gateways")
            .WithName("FinanceReceiveGatewayWebhook")
            .WithSummary("Receive HMAC-verified payment gateway webhook")
            .WithDescription(
                "Verifies gateway HMAC signature and idempotently processes recurring settlement or failure webhooks."
            )
            .Produces<GatewayWebhookProcessResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<
        Results<Ok<IReadOnlyList<RecurringScheduleResponse>>, ProblemHttpResult>
    > ListAsync(
        Guid? personId,
        RecurringScheduleStatus? status,
        RecurringSchedulePurpose? purpose,
        IQueryDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.QueryAsync<
            GetRecurringSchedulesQuery,
            IReadOnlyList<RecurringScheduleResponse>
        >(new GetRecurringSchedulesQuery(personId, status, purpose), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Created<RecurringScheduleResponse>, ProblemHttpResult>
    > CreateAsync(
        CreateRecurringScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CreateRecurringScheduleCommand,
            RecurringScheduleResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/finance/recurring/{result.Value.Id:D}", result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<RecurringScheduleResponse>, ProblemHttpResult>
    > UpdateAsync(
        Guid id,
        UpdateRecurringScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            UpdateRecurringScheduleCommand,
            RecurringScheduleResponse
        >(command with { ScheduleId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<Results<Ok<RecurringScheduleResponse>, ProblemHttpResult>> PauseAsync(
        Guid id,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            PauseRecurringScheduleCommand,
            RecurringScheduleResponse
        >(new PauseRecurringScheduleCommand(id), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<RecurringScheduleResponse>, ProblemHttpResult>
    > ResumeAsync(
        Guid id,
        ResumeRecurringScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ResumeRecurringScheduleCommand,
            RecurringScheduleResponse
        >(command with { ScheduleId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<RecurringScheduleResponse>, ProblemHttpResult>
    > CancelAsync(
        Guid id,
        CancelRecurringScheduleCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            CancelRecurringScheduleCommand,
            RecurringScheduleResponse
        >(command with { ScheduleId = id }, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<RecurringSettlementRunResponse>, ProblemHttpResult>
    > ProcessDueAsync(
        ProcessDueRecurringSchedulesCommand command,
        ICommandDispatcher dispatcher,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await dispatcher.SendAsync<
            ProcessDueRecurringSchedulesCommand,
            RecurringSettlementRunResponse
        >(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }

    private static async Task<
        Results<Ok<GatewayWebhookProcessResponse>, ProblemHttpResult>
    > ReceiveWebhookAsync(
        Guid gatewayId,
        HttpContext httpContext,
        ICommandDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var signature = httpContext.Request.Headers["X-Ruth-Signature"].FirstOrDefault()
            ?? httpContext.Request.Headers["Stripe-Signature"].FirstOrDefault()
            ?? httpContext.Request.Headers["Webhook-Signature"].FirstOrDefault();

        var result = await dispatcher.SendAsync<
            ProcessGatewayWebhookCommand,
            GatewayWebhookProcessResponse
        >(new ProcessGatewayWebhookCommand(gatewayId, rawBody, signature), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : FinanceProblemResultMapper.ToProblemResult(result, httpContext);
    }
}
