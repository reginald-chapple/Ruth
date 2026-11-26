using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Domain.ValueObjects;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>
/// Pure clean-room domain calculator separating tax-deductible charitable receipts from non-deductible dues/fees/invoices,
/// pro-rating partial refunds across splits so refunded portions are never overstated on tax receipts.
/// </summary>
public static class StatementLedgerAggregator
{
    /// <summary>Aggregates settled transactions, invoices, and pledges for a target constituent/household scope.</summary>
    public static AggregatedStatementLedger Aggregate(
        FinancialStatementKind statementKind,
        string currencyCode,
        bool includeOpenInvoices,
        bool includePledgeSummary,
        IReadOnlyCollection<FinancialTransaction> transactions,
        IReadOnlyDictionary<FinancialAccountId, (string Name, string Code)> accountCatalog,
        IReadOnlyCollection<Invoice> invoices,
        IReadOnlyList<StatementPledgeSummaryDto> pledges
    )
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(accountCatalog);
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(pledges);

        _ = IsoCurrencyCatalog.TryGetScale(currencyCode, out var currency, out var scale);

        var deductibleLines = new List<StatementContributionLineDto>();
        var nonDeductibleLines = new List<StatementNonDeductibleLineDto>();
        var qualifyingTxIds = new HashSet<Guid>();

        foreach (var tx in transactions.OrderBy(static t => t.TransactionDateTimeUtc))
        {
            AccumulateTransactionSplits(
                tx,
                statementKind,
                currency,
                scale,
                accountCatalog,
                deductibleLines,
                nonDeductibleLines,
                qualifyingTxIds
            );
        }

        var invoiceSummaries = BuildInvoiceSummaries(
            statementKind,
            currency,
            includeOpenInvoices,
            invoices
        );
        var pledgeSummaries = BuildPledgeSummaries(statementKind, includePledgeSummary, pledges);

        return new AggregatedStatementLedger(
            deductibleLines,
            nonDeductibleLines,
            invoiceSummaries,
            pledgeSummaries,
            deductibleLines.Sum(static x => x.NetDeductibleAmount),
            nonDeductibleLines.Sum(static x => x.NetPaidAmount),
            invoiceSummaries.Sum(static x => x.BalanceDue),
            qualifyingTxIds.Count
        );
    }

    private static void AccumulateTransactionSplits(
        FinancialTransaction tx,
        FinancialStatementKind kind,
        string currency,
        int scale,
        IReadOnlyDictionary<FinancialAccountId, (string Name, string Code)> accountCatalog,
        List<StatementContributionLineDto> deductibleLines,
        List<StatementNonDeductibleLineDto> nonDeductibleLines,
        HashSet<Guid> qualifyingTxIds
    )
    {
        if (!IsEligibleSettledTransaction(tx, currency))
        {
            return;
        }

        var netRatio = tx.NetAmount / tx.TotalAmount;
        foreach (var split in tx.Details)
        {
            AppendSplitLine(
                tx,
                split,
                netRatio,
                kind,
                currency,
                scale,
                accountCatalog,
                deductibleLines,
                nonDeductibleLines,
                qualifyingTxIds
            );
        }
    }

    private static void AppendSplitLine(
        FinancialTransaction tx,
        FinancialTransactionDetail split,
        decimal netRatio,
        FinancialStatementKind kind,
        string currency,
        int scale,
        IReadOnlyDictionary<FinancialAccountId, (string Name, string Code)> accountCatalog,
        List<StatementContributionLineDto> deductibleLines,
        List<StatementNonDeductibleLineDto> nonDeductibleLines,
        HashSet<Guid> qualifyingTxIds
    )
    {
        var netSplitAmount = decimal.Round(
            split.Amount * netRatio,
            scale,
            MidpointRounding.AwayFromZero
        );
        if (netSplitAmount <= 0m)
        {
            return;
        }

        var (accountName, accountCode) = ResolveAccountMeta(split.AccountId, accountCatalog);
        if (split.IsTaxDeductible && IncludesDeductible(kind))
        {
            deductibleLines.Add(
                new StatementContributionLineDto(
                    tx.Id.Value,
                    tx.TransactionDateTimeUtc,
                    accountName,
                    accountCode,
                    tx.TenderType.ToString(),
                    tx.TransactionCode,
                    netSplitAmount,
                    currency
                )
            );
            _ = qualifyingTxIds.Add(tx.Id.Value);
            return;
        }

        if (!split.IsTaxDeductible && IncludesNonDeductible(kind))
        {
            nonDeductibleLines.Add(
                new StatementNonDeductibleLineDto(
                    tx.Id.Value,
                    tx.TransactionDateTimeUtc,
                    accountName,
                    accountCode,
                    split.InvoiceId?.Value,
                    split.Summary ?? tx.Summary,
                    netSplitAmount,
                    currency
                )
            );
            _ = qualifyingTxIds.Add(tx.Id.Value);
        }
    }

    private static bool IsEligibleSettledTransaction(FinancialTransaction tx, string currency)
    {
        return string.Equals(tx.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)
            && tx.TotalAmount > 0m
            && tx.NetAmount > 0m
            && tx.Status
                is FinancialTransactionStatus.Settled
                    or FinancialTransactionStatus.PartiallyRefunded;
    }

    private static bool IncludesDeductible(FinancialStatementKind kind)
    {
        return kind
            is FinancialStatementKind.TaxDeductibleReceipt
                or FinancialStatementKind.CombinedSeparatedStatement;
    }

    private static bool IncludesNonDeductible(FinancialStatementKind kind)
    {
        return kind
            is FinancialStatementKind.NonDeductibleStatement
                or FinancialStatementKind.CombinedSeparatedStatement;
    }

    private static (string Name, string Code) ResolveAccountMeta(
        FinancialAccountId accountId,
        IReadOnlyDictionary<FinancialAccountId, (string Name, string Code)> accountCatalog
    )
    {
        return accountCatalog.TryGetValue(accountId, out var meta)
            ? meta
            : ("Unclassified Account", "UNCLASSIFIED");
    }

    private static List<StatementInvoiceSummaryDto> BuildInvoiceSummaries(
        FinancialStatementKind kind,
        string currency,
        bool includeOpenInvoices,
        IReadOnlyCollection<Invoice> invoices
    )
    {
        return !includeOpenInvoices || !IncludesNonDeductible(kind)
            ? []
            :
            [
                .. invoices
                    .Where(inv =>
                        string.Equals(
                            inv.CurrencyCode,
                            currency,
                            StringComparison.OrdinalIgnoreCase
                        )
                        && inv.Status != Contracts.Invoices.InvoiceStatus.Voided
                        && inv.Status != Contracts.Invoices.InvoiceStatus.Draft
                    )
                    .OrderBy(static inv => inv.IssueDate)
                    .Select(static inv => new StatementInvoiceSummaryDto(
                        inv.Id.Value,
                        inv.InvoiceNumber,
                        inv.IssueDate,
                        inv.DueDate,
                        inv.Status.ToString(),
                        inv.SubtotalAmount,
                        inv.PaidAmount,
                        inv.BalanceDue,
                        inv.CurrencyCode
                    )),
            ];
    }

    private static IReadOnlyList<StatementPledgeSummaryDto> BuildPledgeSummaries(
        FinancialStatementKind kind,
        bool includePledgeSummary,
        IReadOnlyList<StatementPledgeSummaryDto> pledges
    )
    {
        return includePledgeSummary && IncludesDeductible(kind) ? pledges : [];
    }
}
