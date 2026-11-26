using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialTransactionRefund" />.</summary>
public readonly record struct FinancialTransactionRefundId(Guid Value)
    : IStronglyTypedId<FinancialTransactionRefundId>
{
    /// <inheritdoc />
    public static FinancialTransactionRefundId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialTransactionRefundId Create()
    {
        return new FinancialTransactionRefundId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialTransactionRefundId From(Guid value)
    {
        return new FinancialTransactionRefundId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
