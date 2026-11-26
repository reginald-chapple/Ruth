using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class ProcessGatewayWebhookCommandHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry gatewayRegistry,
    GatewayWebhookSettlementCollaborator webhookCollaborator,
    IClock clock
) : ICommandHandler<ProcessGatewayWebhookCommand, GatewayWebhookProcessResponse>
{
    public async Task<Result<GatewayWebhookProcessResponse>> HandleAsync(
        ProcessGatewayWebhookCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var gatewayId = FinancialGatewayId.From(command.GatewayId);
        var verified = await VerifyGatewayWebhookAsync(gatewayId, command, cancellationToken);
        if (verified.IsFailure)
        {
            return Result.Failure<GatewayWebhookProcessResponse>(verified.Errors);
        }

        var payload = verified.Value;
        var alreadyProcessed = await dbContext
            .ProcessedGatewayWebhooks.AsNoTracking()
            .AnyAsync(
                w =>
                    w.FinancialGatewayId == gatewayId && w.WebhookEventId == payload.WebhookEventId,
                cancellationToken
            );
        if (alreadyProcessed)
        {
            return new GatewayWebhookProcessResponse(
                payload.WebhookEventId,
                payload.EventKind,
                WasDuplicate: true,
                RecordedTransactionId: null,
                AffectedScheduleId: null
            );
        }

        _ = dbContext.ProcessedGatewayWebhooks.Add(
            ProcessedGatewayWebhook.Create(
                gatewayId,
                payload.WebhookEventId,
                payload.EventKind,
                clock.UtcNow
            )
        );

        var (txId, scheduleId) = await webhookCollaborator.ApplyMutationAsync(
            gatewayId,
            payload,
            cancellationToken
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new GatewayWebhookProcessResponse(
            payload.WebhookEventId,
            payload.EventKind,
            WasDuplicate: false,
            txId,
            scheduleId
        );
    }

    private async Task<Result<Contracts.Gateways.GatewayWebhookPayload>> VerifyGatewayWebhookAsync(
        FinancialGatewayId gatewayId,
        ProcessGatewayWebhookCommand command,
        CancellationToken cancellationToken
    )
    {
        var gateway = await dbContext
            .FinancialGateways.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gatewayId, cancellationToken);
        return gateway is null ? FinanceErrors.GatewayNotFound
            : !gatewayRegistry.TryGet(gateway.ProviderKey, out var provider)
                ? FinanceErrors.GatewayProviderUnavailable
            : provider.ParseAndVerifyWebhook(command.RawBody, command.SignatureHeader);
    }
}
