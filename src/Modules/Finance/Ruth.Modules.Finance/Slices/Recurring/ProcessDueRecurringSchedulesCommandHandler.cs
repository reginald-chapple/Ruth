using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Recurring;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Recurring;

internal sealed class ProcessDueRecurringSchedulesCommandHandler(
    FinanceDbContext dbContext,
    ICommandDispatcher commandDispatcher,
    IClock clock
) : ICommandHandler<ProcessDueRecurringSchedulesCommand, RecurringSettlementRunResponse>
{
    public async Task<Result<RecurringSettlementRunResponse>> HandleAsync(
        ProcessDueRecurringSchedulesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var asOfDate = command.AsOfDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var limit = Math.Clamp(command.MaxBatchSize, 1, 1000);

        var dueSchedules = await dbContext
            .RecurringSchedules.Include(static s => s.Splits)
            .Where(s => s.Status == RecurringScheduleStatus.Active && s.NextPaymentDate <= asOfDate)
            .OrderBy(static s => s.NextPaymentDate)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (dueSchedules.Count == 0)
        {
            return new RecurringSettlementRunResponse(0, 0, 0, 0m);
        }

        var settledCount = 0;
        var failedCount = 0;
        var settledTotal = 0m;

        foreach (var schedule in dueSchedules)
        {
            var settled = await ProcessSingleDueScheduleAsync(
                schedule,
                asOfDate,
                cancellationToken
            );
            if (settled)
            {
                settledCount++;
                settledTotal += schedule.TotalAmount;
            }
            else
            {
                failedCount++;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new RecurringSettlementRunResponse(
            dueSchedules.Count,
            settledCount,
            failedCount,
            settledTotal
        );
    }

    private async Task<bool> ProcessSingleDueScheduleAsync(
        RecurringScheduleTransaction schedule,
        DateOnly asOfDate,
        CancellationToken cancellationToken
    )
    {
        var methodKind = await dbContext
            .SavedPaymentMethods.AsNoTracking()
            .Where(m => m.Id == schedule.SavedPaymentMethodId)
            .Select(static m => (PaymentMethodKind?)m.MethodKind)
            .FirstOrDefaultAsync(cancellationToken)
            ?? PaymentMethodKind.CreditCard;

        var batchId = await GetOrCreateAutomatedBatchIdAsync(
            asOfDate,
            schedule.CurrencyCode,
            cancellationToken
        );

        var purposeText = schedule.Purpose.ToString();
        var frequencyText = schedule.Frequency.ToString();
        var recordCmd = new RecordFinancialTransactionCommand(
            schedule.PersonId,
            schedule.CurrencyCode,
            FinancialTransactionSourceType.RecurringEngine,
            methodKind == PaymentMethodKind.AchBankAccount
                ? FinancialTenderType.Ach
                : FinancialTenderType.CreditCard,
            [
                .. schedule.Splits.Select(static s => new FinancialTransactionSplitSpec(
                    s.AccountId.Value,
                    s.Amount,
                    InvoiceId: null,
                    s.Summary
                )),
            ],
            BatchId: batchId,
            FinancialGatewayId: schedule.FinancialGatewayId.Value,
            SavedPaymentMethodId: schedule.SavedPaymentMethodId.Value,
            HouseholdId: schedule.HouseholdId,
            RecurringScheduleId: schedule.Id.Value,
            Summary: $"Recurring {purposeText} ({frequencyText})"
        );

        var txResult = await commandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(recordCmd, cancellationToken);
        if (txResult.IsFailure)
        {
            schedule.RecordFailedAttempt(txResult.PrimaryError.Description, clock.UtcNow);
            return false;
        }

        schedule.AdvanceAfterSettlement(schedule.NextPaymentDate, clock.UtcNow);
        return true;
    }

    private async Task<Guid?> GetOrCreateAutomatedBatchIdAsync(
        DateOnly asOfDate,
        string currency,
        CancellationToken cancellationToken
    )
    {
        var batchName = string.Create(
            CultureInfo.InvariantCulture,
            $"Automated Recurring Settlement {asOfDate:yyyy-MM-dd}"
        );
        var existing = await dbContext
            .FinancialBatches.AsNoTracking()
            .Where(b =>
                b.Status == FinancialBatchStatus.Open
                && b.IsAutomated
                && b.Name == batchName
                && b.CurrencyCode == currency
            )
            .Select(static b => (Guid?)b.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = FinancialBatch.Create(
            batchName,
            currency,
            controlAmount: 0m,
            controlItemCount: 0,
            clock.UtcNow,
            isAutomated: true,
            openImmediately: true
        );
        if (created.IsFailure)
        {
            return null;
        }

        _ = dbContext.FinancialBatches.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return created.Value.Id.Value;
    }
}
