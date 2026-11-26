using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class IngestScannedCheckCommandHandler(
    FinanceDbContext dbContext,
    TransactionPostingCollaborator collaborator,
    ScannedCheckImageAttacher imageAttacher,
    IOptions<FinanceSecurityOptions> securityOptions,
    IClock clock
) : ICommandHandler<IngestScannedCheckCommand, CheckScannerIngestResponse>
{
    public async Task<Result<CheckScannerIngestResponse>> HandleAsync(
        IngestScannedCheckCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var hmacKey = securityOptions.Value.GetBankAccountHmacKeyBytes();
        var digest = BankAccountIdentifierHasher.ComputeDigest(
            command.RoutingNumber,
            command.AccountNumber,
            hmacKey
        );
        if (digest.IsFailure)
        {
            return Result.Failure<CheckScannerIngestResponse>(digest.Errors);
        }

        var matchedPersonId = await ResolveOrBindPersonAsync(
            command,
            digest.Value.HashHex,
            hmacKey,
            cancellationToken
        );

        var batchResult = await ResolveOrCreateScannerBatchAsync(command, cancellationToken);
        return batchResult.IsFailure
            ? Result.Failure<CheckScannerIngestResponse>(batchResult.Errors)
            : await PostScannedCheckAsync(
                command,
                batchResult.Value,
                matchedPersonId,
                digest.Value.HashHex,
                cancellationToken
            );
    }

    private async Task<Result<CheckScannerIngestResponse>> PostScannedCheckAsync(
        IngestScannedCheckCommand command,
        FinancialBatch batch,
        Guid? matchedPersonId,
        string hashHex,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var splitsResult = await collaborator.ResolveSplitsAndApplyInvoicesAsync(
            [
                new FinancialTransactionSplitSpec(
                    command.AccountId,
                    command.Amount,
                    command.InvoiceId,
                    command.Summary
                ),
            ],
            DateOnly.FromDateTime(nowUtc.UtcDateTime),
            cancellationToken
        );
        if (splitsResult.IsFailure)
        {
            return Result.Failure<CheckScannerIngestResponse>(splitsResult.Errors);
        }

        var txResult = FinancialTransaction.Create(
            command.CurrencyCode,
            nowUtc,
            FinancialTransactionSourceType.BankCheckScanner,
            FinancialTenderType.Check,
            FinancialTransactionStatus.Settled,
            splitsResult.Value.ResolvedSplits,
            matchedPersonId,
            batchId: batch.Id,
            transactionCode: command.CheckNumber,
            summary: command.Summary,
            checkMicrHash: hashHex
        );
        return txResult.IsFailure
            ? Result.Failure<CheckScannerIngestResponse>(txResult.Errors)
            : await FinalizeScannedCheckAsync(
                command,
                batch,
                txResult.Value,
                splitsResult.Value.AccountNames,
                matchedPersonId,
                hashHex,
                cancellationToken
            );
    }

    private async Task<Result<CheckScannerIngestResponse>> FinalizeScannedCheckAsync(
        IngestScannedCheckCommand command,
        FinancialBatch batch,
        FinancialTransaction transaction,
        Dictionary<Domain.Accounts.FinancialAccountId, string> accountNames,
        Guid? matchedPersonId,
        string hashHex,
        CancellationToken cancellationToken
    )
    {
        var imgResult = await imageAttacher.AttachOptionalImagesAsync(
            transaction,
            command.FrontImageBase64,
            command.BackImageBase64,
            command.ImageContentType,
            cancellationToken
        );
        if (imgResult.IsFailure)
        {
            return Result.Failure<CheckScannerIngestResponse>(imgResult.Errors);
        }

        if (batch.IsAutomated)
        {
            _ = batch.IncrementAutomatedControl(transaction.TotalAmount);
        }

        _ = dbContext.FinancialTransactions.Add(transaction);
        collaborator.EnqueueTransactionRecordedEvent(transaction);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new CheckScannerIngestResponse(
            FinancialTransactionMapper.ToResponse(transaction, accountNames),
            batch.Id.Value,
            matchedPersonId,
            hashHex
        );
    }

    private async Task<Guid?> ResolveOrBindPersonAsync(
        IngestScannedCheckCommand command,
        string hashHex,
        byte[] hmacKey,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.BankAccountIdentifiers.FirstOrDefaultAsync(
            b => b.RoutingAndAccountHash == hashHex,
            cancellationToken
        );
        var effectivePersonId =
            command.OverridePersonId is { } over && over != Guid.Empty ? over : existing?.PersonId;

        if (command.BindBankIdentifierToPerson && effectivePersonId is { } pid && existing is null)
        {
            var created = BankAccountIdentifier.Create(
                pid,
                command.RoutingNumber,
                command.AccountNumber,
                hmacKey,
                clock.UtcNow,
                "Check Scanner Auto-Bound"
            );
            if (created.IsSuccess)
            {
                _ = dbContext.BankAccountIdentifiers.Add(created.Value);
            }
        }

        return effectivePersonId;
    }

    private async Task<Result<FinancialBatch>> ResolveOrCreateScannerBatchAsync(
        IngestScannedCheckCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.BatchId is { } explicitGuid && explicitGuid != Guid.Empty)
        {
            var check = await collaborator.ResolveBatchAsync(explicitGuid, cancellationToken);
            return check.IsFailure
                ? Result.Failure<FinancialBatch>(check.Errors)
                : Result.Create(check.Value.Batch);
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var defaultName = string.Create(
            CultureInfo.InvariantCulture,
            $"Check Scanner Batch {today:yyyy-MM-dd}"
        );
        var normalizedCurrency = command.CurrencyCode.Trim().ToUpperInvariant();
        var openScannerBatch = await dbContext.FinancialBatches.FirstOrDefaultAsync(
            b =>
                b.Status == FinancialBatchStatus.Open
                && b.IsAutomated
                && b.Name == defaultName
                && b.CurrencyCode == normalizedCurrency,
            cancellationToken
        );
        if (openScannerBatch is not null)
        {
            return openScannerBatch;
        }

        var created = FinancialBatch.Create(
            defaultName,
            normalizedCurrency,
            controlAmount: 0m,
            controlItemCount: 0,
            clock.UtcNow,
            isAutomated: true,
            openImmediately: true
        );
        if (created.IsSuccess)
        {
            _ = dbContext.FinancialBatches.Add(created.Value);
        }

        return created;
    }
}
