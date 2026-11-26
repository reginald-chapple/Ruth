using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Account and optional invoice allocation split belonging to a <see cref="FinancialTransaction" />.</summary>
public sealed class FinancialTransactionDetail : Entity<FinancialTransactionDetailId>
{
    private FinancialTransactionDetail() { }

    internal FinancialTransactionDetail(
        FinancialTransactionDetailId id,
        FinancialTransactionId transactionId,
        FinancialAccountId accountId,
        decimal amount,
        InvoiceId? invoiceId,
        bool isTaxDeductible,
        string? summary
    )
        : base(id)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        Amount = amount;
        InvoiceId = invoiceId;
        IsTaxDeductible = isTaxDeductible;
        Summary = summary;
    }

    public FinancialTransactionId TransactionId { get; private set; }
    public FinancialAccountId AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public InvoiceId? InvoiceId { get; private set; }
    public bool IsTaxDeductible { get; private set; }
    public string? Summary { get; private set; }
}
