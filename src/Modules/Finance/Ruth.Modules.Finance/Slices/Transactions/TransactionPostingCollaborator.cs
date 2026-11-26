using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Batches;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Transactions;

internal sealed class TransactionPostingCollaborator(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry gatewayRegistry,
    IClock clock
)
{
    public async Task<Result<(FinancialBatch? Batch, FinancialBatchId? BatchId)>> ResolveBatchAsync(
        Guid? batchGuid,
        CancellationToken cancellationToken
    )
    {
        if (batchGuid is null || batchGuid.Value == Guid.Empty)
        {
            return (null, null);
        }

        var batchId = FinancialBatchId.From(batchGuid.Value);
        var batch = await dbContext.FinancialBatches.FirstOrDefaultAsync(
            b => b.Id == batchId,
            cancellationToken
        );
        if (batch is null)
        {
            return FinanceErrors.BatchNotFound;
        }

        var canPost = batch.EnsureCanPostTransactions();
        return canPost.IsFailure
            ? Result.Failure<(FinancialBatch?, FinancialBatchId?)>(canPost.Errors)
            : (batch, batchId);
    }

    public async Task<
        Result<(
            List<(
                FinancialAccountId AccountId,
                decimal Amount,
                InvoiceId? InvoiceId,
                bool IsTaxDeductible,
                string? Summary
            )> ResolvedSplits,
            Dictionary<FinancialAccountId, string> AccountNames,
            decimal TotalAmount
        )>
    > ResolveSplitsAndApplyInvoicesAsync(
        IReadOnlyList<FinancialTransactionSplitSpec>? specs,
        DateOnly effectiveDate,
        CancellationToken cancellationToken
    )
    {
        if (specs is null || specs.Count == 0)
        {
            return FinanceErrors.TransactionSplitsInvalid;
        }

        var accountIds = specs
            .Select(static s => FinancialAccountId.From(s.AccountId))
            .Distinct()
            .ToArray();
        var accounts = await dbContext
            .FinancialAccounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(static a => a.Id, cancellationToken);

        var resolved = new List<(FinancialAccountId, decimal, InvoiceId?, bool, string?)>(
            specs.Count
        );
        var total = 0m;

        foreach (var spec in specs)
        {
            var accountId = FinancialAccountId.From(spec.AccountId);
            if (!accounts.TryGetValue(accountId, out var account))
            {
                return FinanceErrors.AccountNotFound;
            }

            if (!account.IsOpenOn(effectiveDate))
            {
                return FinanceErrors.AccountClosedForDate;
            }

            var invCheck = await ApplyInvoicePaymentIfSpecifiedAsync(
                spec.InvoiceId,
                spec.Amount,
                cancellationToken
            );
            if (invCheck.IsFailure)
            {
                return Result.Failure<(
                    List<(FinancialAccountId, decimal, InvoiceId?, bool, string?)>,
                    Dictionary<FinancialAccountId, string>,
                    decimal
                )>(invCheck.Errors);
            }

            resolved.Add(
                (accountId, spec.Amount, invCheck.Value, account.IsTaxDeductible, spec.Summary)
            );
            total += spec.Amount;
        }

        var names = accounts.ToDictionary(static kv => kv.Key, static kv => kv.Value.Name);
        return (resolved, names, total);
    }

    public async Task<
        Result<(
            FinancialGatewayId? GatewayId,
            SavedPaymentMethodId? SavedMethodId,
            string? TransactionCode,
            FinancialTransactionStatus Status
        )>
    > ExecuteGatewayChargeIfRequestedAsync(
        RecordFinancialTransactionCommand command,
        decimal totalAmount,
        CancellationToken cancellationToken
    )
    {
        if (command.FinancialGatewayId is null || command.FinancialGatewayId.Value == Guid.Empty)
        {
            return (null, null, command.TransactionCode, FinancialTransactionStatus.Settled);
        }

        var gatewayResult = await ResolveActiveGatewayProviderAsync(
            command.FinancialGatewayId.Value,
            cancellationToken
        );
        if (gatewayResult.IsFailure)
        {
            return Result.Failure<(
                FinancialGatewayId?,
                SavedPaymentMethodId?,
                string?,
                FinancialTransactionStatus
            )>(gatewayResult.Errors);
        }

        var vaultInfo = await ResolveVaultTokenAsync(
            command.SavedPaymentMethodId,
            cancellationToken
        );
        return vaultInfo.IsFailure
            ? Result.Failure<(
                FinancialGatewayId?,
                SavedPaymentMethodId?,
                string?,
                FinancialTransactionStatus
            )>(vaultInfo.Errors)
            : await InvokeProviderChargeAsync(
                command,
                totalAmount,
                gatewayResult.Value,
                vaultInfo.Value,
                cancellationToken
            );
    }

    private static async Task<
        Result<(
            FinancialGatewayId? GatewayId,
            SavedPaymentMethodId? SavedMethodId,
            string? TransactionCode,
            FinancialTransactionStatus Status
        )>
    > InvokeProviderChargeAsync(
        RecordFinancialTransactionCommand command,
        decimal totalAmount,
        (FinancialGatewayId GatewayId, bool IsTestMode, IPaymentGateway Provider) gatewayInfo,
        (SavedPaymentMethodId? SavedId, string? VaultToken) vaultInfo,
        CancellationToken cancellationToken
    )
    {
        var kind =
            command.TenderType == FinancialTenderType.Ach
                ? PaymentMethodKind.AchBankAccount
                : PaymentMethodKind.CreditCard;

        var charge = await gatewayInfo.Provider.ChargeAsync(
            new GatewayChargeRequest(
                totalAmount,
                command.CurrencyCode,
                vaultInfo.VaultToken,
                command.OneTimeHostedToken,
                kind,
                Guid.CreateVersion7().ToString("N"),
                command.Summary,
                gatewayInfo.IsTestMode
            ),
            cancellationToken
        );
        return charge.IsFailure
            ? Result.Failure<(
                FinancialGatewayId?,
                SavedPaymentMethodId?,
                string?,
                FinancialTransactionStatus
            )>(charge.Errors)
            : (
                gatewayInfo.GatewayId,
                vaultInfo.SavedId,
                charge.Value.GatewayTransactionId,
                charge.Value.IsSettledImmediately
                    ? FinancialTransactionStatus.Settled
                    : FinancialTransactionStatus.Pending
            );
    }

    public void EnqueueTransactionRecordedEvent(FinancialTransaction tx)
    {
        var deductible = tx.Details.Where(static d => d.IsTaxDeductible).Sum(static d => d.Amount);
        dbContext.EnqueueIntegrationEvent(
            new FinancialTransactionRecordedIntegrationEvent(
                Guid.CreateVersion7(),
                clock.UtcNow,
                tx.Id.Value,
                tx.AuthorizedPersonId,
                tx.TotalAmount,
                tx.CurrencyCode,
                deductible,
                tx.TotalAmount - deductible,
                tx.SourceType,
                tx.TenderType
            )
        );
    }

    private async Task<
        Result<(FinancialGatewayId Id, bool IsTestMode, IPaymentGateway Provider)>
    > ResolveActiveGatewayProviderAsync(Guid gatewayGuid, CancellationToken cancellationToken)
    {
        var gatewayId = FinancialGatewayId.From(gatewayGuid);
        var gateway = await dbContext
            .FinancialGateways.AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gatewayId, cancellationToken);
        return gateway is null ? FinanceErrors.GatewayNotFound
            : !gateway.IsActive ? FinanceErrors.GatewayInactive
            : !gatewayRegistry.TryGet(gateway.ProviderKey, out var provider)
                ? FinanceErrors.GatewayProviderUnavailable
            : (gatewayId, gateway.IsTestMode, provider);
    }

    private async Task<Result<InvoiceId?>> ApplyInvoicePaymentIfSpecifiedAsync(
        Guid? invoiceGuid,
        decimal amount,
        CancellationToken cancellationToken
    )
    {
        if (invoiceGuid is null || invoiceGuid.Value == Guid.Empty)
        {
            return Result.Success<InvoiceId?>(value: null);
        }

        var invoiceId = InvoiceId.From(invoiceGuid.Value);
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(
            i => i.Id == invoiceId,
            cancellationToken
        );
        if (invoice is null)
        {
            return FinanceErrors.InvoiceNotFound;
        }

        var paidResult = invoice.ApplyPayment(amount);
        if (paidResult.IsFailure)
        {
            return Result.Failure<InvoiceId?>(paidResult.Errors);
        }

        if (paidResult.Value)
        {
            dbContext.EnqueueIntegrationEvent(
                new InvoiceSettledIntegrationEvent(
                    Guid.CreateVersion7(),
                    clock.UtcNow,
                    invoice.Id.Value,
                    invoice.InvoiceNumber,
                    invoice.PersonId,
                    invoice.PaidAmount,
                    invoice.CurrencyCode
                )
            );
        }

        return invoiceId;
    }

    private async Task<
        Result<(SavedPaymentMethodId? SavedId, string? VaultToken)>
    > ResolveVaultTokenAsync(Guid? savedMethodGuid, CancellationToken cancellationToken)
    {
        if (savedMethodGuid is null || savedMethodGuid.Value == Guid.Empty)
        {
            return (null, null);
        }

        var savedId = SavedPaymentMethodId.From(savedMethodGuid.Value);
        var saved = await dbContext
            .SavedPaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == savedId && m.IsActive, cancellationToken);
        return saved is null
            ? FinanceErrors.PaymentMethodNotFound
            : (savedId, saved.CustomerVaultToken);
    }
}
