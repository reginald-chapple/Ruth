using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Slices.Transactions;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class GatewayWebhookSettlementCollaborator(
    FinanceDbContext dbContext,
    TransactionPostingCollaborator postingCollaborator,
    IClock clock
)
{
    public async Task<(Guid? TxId, Guid? ScheduleId)> ApplyMutationAsync(
        FinancialGatewayId gatewayId,
        GatewayWebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(payload.GatewaySubscriptionReference))
        {
            return (null, null);
        }

        var schedule = await dbContext
            .RecurringSchedules.Include(static s => s.Splits)
            .FirstOrDefaultAsync(
                s =>
                    s.FinancialGatewayId == gatewayId
                    && s.GatewaySubscriptionReference == payload.GatewaySubscriptionReference,
                cancellationToken
            );
        if (schedule is null)
        {
            return (null, null);
        }

        return payload.EventKind switch
        {
            GatewayWebhookEventKind.SubscriptionChargeSucceeded => await RecordSettlementAsync(
                schedule,
                payload,
                cancellationToken
            ),
            GatewayWebhookEventKind.SubscriptionChargeFailed => RecordFailure(schedule, payload),
            GatewayWebhookEventKind.SubscriptionCancelled => RecordCancellation(schedule),
            GatewayWebhookEventKind.ChargeRefunded => (null, schedule.Id.Value),
            _ => (null, schedule.Id.Value),
        };
    }

    private async Task<(Guid? TxId, Guid? ScheduleId)> RecordSettlementAsync(
        RecurringScheduleTransaction schedule,
        GatewayWebhookPayload payload,
        CancellationToken cancellationToken
    )
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var deductibilityMap = await LoadDeductibilityMapAsync(schedule, cancellationToken);
        var batch = await GetOrCreateWebhookBatchAsync(
            today,
            schedule.CurrencyCode,
            cancellationToken
        );
        var txResult = BuildWebhookTransaction(schedule, payload, deductibilityMap, batch?.Id);
        if (txResult.IsFailure)
        {
            return (null, schedule.Id.Value);
        }

        if (batch is not null)
        {
            _ = batch.IncrementAutomatedControl(txResult.Value.TotalAmount);
        }

        _ = dbContext.FinancialTransactions.Add(txResult.Value);
        postingCollaborator.EnqueueTransactionRecordedEvent(txResult.Value);
        schedule.AdvanceAfterSettlement(schedule.NextPaymentDate, payload.OccurredAtUtc);
        return (txResult.Value.Id.Value, schedule.Id.Value);
    }

    private async Task<
        Dictionary<Domain.Accounts.FinancialAccountId, bool>
    > LoadDeductibilityMapAsync(
        RecurringScheduleTransaction schedule,
        CancellationToken cancellationToken
    )
    {
        var accountIds = schedule.Splits.Select(static s => s.AccountId).Distinct().ToArray();
        return await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, static a => a.IsTaxDeductible, cancellationToken);
    }

    private static SharedKernel.Results.Result<FinancialTransaction> BuildWebhookTransaction(
        RecurringScheduleTransaction schedule,
        GatewayWebhookPayload payload,
        Dictionary<Domain.Accounts.FinancialAccountId, bool> deductibilityMap,
        FinancialBatchId? batchId
    )
    {
        return FinancialTransaction.Create(
            schedule.CurrencyCode,
            payload.OccurredAtUtc,
            FinancialTransactionSourceType.RecurringEngine,
            FinancialTenderType.CreditCard,
            FinancialTransactionStatus.Settled,
            [
                .. schedule.Splits.Select(s =>
                    (
                        s.AccountId,
                        s.Amount,
                        (InvoiceId?)null,
                        deductibilityMap.GetValueOrDefault(s.AccountId),
                        s.Summary
                    )
                ),
            ],
            schedule.PersonId,
            schedule.HouseholdId,
            batchId,
            schedule.FinancialGatewayId,
            schedule.SavedPaymentMethodId,
            schedule.Id.Value,
            payload.GatewayTransactionId ?? payload.WebhookEventId,
            "Gateway Webhook Recurring Settlement"
        );
    }

    private static (Guid? TxId, Guid? ScheduleId) RecordFailure(
        RecurringScheduleTransaction schedule,
        GatewayWebhookPayload payload
    )
    {
        schedule.RecordFailedAttempt(payload.FailureMessage, payload.OccurredAtUtc);
        return (null, schedule.Id.Value);
    }

    private static (Guid? TxId, Guid? ScheduleId) RecordCancellation(
        RecurringScheduleTransaction schedule
    )
    {
        _ = schedule.Cancel("Cancelled via gateway webhook");
        return (null, schedule.Id.Value);
    }

    private async Task<FinancialBatch?> GetOrCreateWebhookBatchAsync(
        DateOnly today,
        string currency,
        CancellationToken cancellationToken
    )
    {
        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"Gateway Webhook Settlement {today:yyyy-MM-dd}"
        );
        var existing = await dbContext.FinancialBatches.FirstOrDefaultAsync(
            b =>
                b.Status == FinancialBatchStatus.Open
                && b.IsAutomated
                && b.Name == name
                && b.CurrencyCode == currency,
            cancellationToken
        );
        if (existing is not null)
        {
            return existing;
        }

        var created = FinancialBatch.Create(
            name,
            currency,
            0m,
            0,
            clock.UtcNow,
            isAutomated: true,
            openImmediately: true
        );
        if (created.IsFailure)
        {
            return null;
        }

        _ = dbContext.FinancialBatches.Add(created.Value);
        return created.Value;
    }
}
