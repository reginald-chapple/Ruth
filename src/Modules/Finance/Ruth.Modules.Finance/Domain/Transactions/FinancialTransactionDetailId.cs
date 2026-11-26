using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialTransactionDetail" />.</summary>
public readonly record struct FinancialTransactionDetailId(Guid Value)
    : IStronglyTypedId<FinancialTransactionDetailId>
{
    /// <inheritdoc />
    public static FinancialTransactionDetailId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialTransactionDetailId Create()
    {
        return new FinancialTransactionDetailId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialTransactionDetailId From(Guid value)
    {
        return new FinancialTransactionDetailId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
