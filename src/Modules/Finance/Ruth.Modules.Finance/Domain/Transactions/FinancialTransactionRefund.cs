using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Full or partial refund record attached to a <see cref="FinancialTransaction" />.</summary>
public sealed class FinancialTransactionRefund : Entity<FinancialTransactionRefundId>
{
    private FinancialTransactionRefund() { }

    internal FinancialTransactionRefund(
        FinancialTransactionRefundId id,
        FinancialTransactionId transactionId,
        decimal amount,
        string reason,
        string? gatewayRefundReference,
        Guid? processedByPersonId,
        DateTimeOffset refundedAtUtc
    )
        : base(id)
    {
        TransactionId = transactionId;
        Amount = amount;
        Reason = reason;
        GatewayRefundReference = gatewayRefundReference;
        ProcessedByPersonId = processedByPersonId;
        RefundedAtUtc = refundedAtUtc.ToUniversalTime();
    }

    public FinancialTransactionId TransactionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? GatewayRefundReference { get; private set; }
    public Guid? ProcessedByPersonId { get; private set; }
    public DateTimeOffset RefundedAtUtc { get; private set; }
}
