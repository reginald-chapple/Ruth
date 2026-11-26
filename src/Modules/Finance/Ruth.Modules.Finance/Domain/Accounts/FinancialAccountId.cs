using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Accounts;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialAccount" />.</summary>
public readonly record struct FinancialAccountId(Guid Value) : IStronglyTypedId<FinancialAccountId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialAccountId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialAccountId Create()
    {
        return new FinancialAccountId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialAccountId From(Guid value)
    {
        return new FinancialAccountId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
