namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Classifies the financial and tax-reporting purpose of a statement template.</summary>
public enum FinancialStatementKind
{
    /// <summary>Charitable contribution receipt restricted strictly to tax-deductible funds and grants.</summary>
    TaxDeductibleReceipt = 0,

    /// <summary>Account statement restricted to non-deductible membership dues, program fees, and invoices.</summary>
    NonDeductibleStatement = 1,

    /// <summary>Unified constituent statement strictly separating tax-deductible receipts from non-deductible account activity.</summary>
    CombinedSeparatedStatement = 2,
}
