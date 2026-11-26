using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Transactions;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialTransaction" />.</summary>
public readonly record struct FinancialTransactionId(Guid Value)
    : IStronglyTypedId<FinancialTransactionId>
{
    /// <inheritdoc />
    public static FinancialTransactionId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialTransactionId Create()
    {
        return new FinancialTransactionId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialTransactionId From(Guid value)
    {
        return new FinancialTransactionId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
