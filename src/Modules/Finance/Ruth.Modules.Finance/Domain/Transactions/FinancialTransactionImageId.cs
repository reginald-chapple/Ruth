using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialTransactionImage" />.</summary>
public readonly record struct FinancialTransactionImageId(Guid Value)
    : IStronglyTypedId<FinancialTransactionImageId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialTransactionImageId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialTransactionImageId Create()
    {
        return new FinancialTransactionImageId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialTransactionImageId From(Guid value)
    {
        return new FinancialTransactionImageId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
