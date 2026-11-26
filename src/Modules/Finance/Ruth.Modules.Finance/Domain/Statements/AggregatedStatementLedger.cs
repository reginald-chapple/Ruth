using Ruth.Modules.Finance.Contracts.Statements;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>Aggregated ledger breakdown separating tax-deductible receipts from non-deductible dues, fees, and invoices.</summary>
public sealed record AggregatedStatementLedger(
    IReadOnlyList<StatementContributionLineDto> TaxDeductibleLines,
    IReadOnlyList<StatementNonDeductibleLineDto> NonDeductibleLines,
    IReadOnlyList<StatementInvoiceSummaryDto> InvoiceSummaries,
    IReadOnlyList<StatementPledgeSummaryDto> PledgeSummaries,
    decimal TaxDeductibleTotal,
    decimal NonDeductiblePaidTotal,
    decimal OpenInvoiceBalanceTotal,
    int QualifyingTransactionCount
)
{
    /// <summary>Total qualifying statement value relevant to the requested <see cref="FinancialStatementKind" />.</summary>
    public decimal RelevantActivityTotal(FinancialStatementKind kind)
    {
        return kind switch
        {
            FinancialStatementKind.TaxDeductibleReceipt => TaxDeductibleTotal,
            FinancialStatementKind.NonDeductibleStatement => NonDeductiblePaidTotal
                + OpenInvoiceBalanceTotal,
            FinancialStatementKind.CombinedSeparatedStatement => TaxDeductibleTotal
                + NonDeductiblePaidTotal
                + OpenInvoiceBalanceTotal,
            _ => TaxDeductibleTotal + NonDeductiblePaidTotal + OpenInvoiceBalanceTotal,
        };
    }
}
