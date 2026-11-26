using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class RecordFinancialTransactionCommandHandler(
    FinanceDbContext dbContext,
    TransactionPostingCollaborator collaborator,
    IClock clock
) : ICommandHandler<RecordFinancialTransactionCommand, FinancialTransactionResponse>
{
    public async Task<Result<FinancialTransactionResponse>> HandleAsync(
        RecordFinancialTransactionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var batchCheck = await collaborator.ResolveBatchAsync(command.BatchId, cancellationToken);
        if (batchCheck.IsFailure)
        {
            return Result.Failure<FinancialTransactionResponse>(batchCheck.Errors);
        }

        var effectiveUtc = (command.TransactionDateTimeUtc ?? clock.UtcNow).ToUniversalTime();
        var splitsResult = await collaborator.ResolveSplitsAndApplyInvoicesAsync(
            command.Splits,
            DateOnly.FromDateTime(effectiveUtc.UtcDateTime),
            cancellationToken
        );
        if (splitsResult.IsFailure)
        {
            return Result.Failure<FinancialTransactionResponse>(splitsResult.Errors);
        }

        var gatewayResult = await collaborator.ExecuteGatewayChargeIfRequestedAsync(
            command,
            splitsResult.Value.TotalAmount,
            cancellationToken
        );
        if (gatewayResult.IsFailure)
        {
            return Result.Failure<FinancialTransactionResponse>(gatewayResult.Errors);
        }

        var created = CreateTransactionAggregate(
            command,
            effectiveUtc,
            splitsResult.Value.ResolvedSplits,
            batchCheck.Value.BatchId,
            gatewayResult.Value
        );
        if (created.IsFailure)
        {
            return Result.Failure<FinancialTransactionResponse>(created.Errors);
        }

        if (batchCheck.Value.Batch is { IsAutomated: true } autoBatch)
        {
            _ = autoBatch.IncrementAutomatedControl(created.Value.TotalAmount);
        }

        _ = dbContext.FinancialTransactions.Add(created.Value);
        collaborator.EnqueueTransactionRecordedEvent(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return FinancialTransactionMapper.ToResponse(
            created.Value,
            splitsResult.Value.AccountNames
        );
    }

    private static Result<FinancialTransaction> CreateTransactionAggregate(
        RecordFinancialTransactionCommand command,
        DateTimeOffset effectiveUtc,
        List<(FinancialAccountId, decimal, InvoiceId?, bool, string?)> splits,
        FinancialBatchId? batchId,
        (
            FinancialGatewayId? GatewayId,
            SavedPaymentMethodId? SavedMethodId,
            string? TransactionCode,
            FinancialTransactionStatus Status
        ) gateway
    )
    {
        return FinancialTransaction.Create(
            command.CurrencyCode,
            effectiveUtc,
            command.SourceType,
            command.TenderType,
            gateway.Status,
            splits,
            command.AuthorizedPersonId,
            command.HouseholdId,
            batchId,
            gateway.GatewayId,
            gateway.SavedMethodId,
            command.RecurringScheduleId,
            gateway.TransactionCode,
            command.Summary
        );
    }
}
