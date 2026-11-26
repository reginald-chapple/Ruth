using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Pledges;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialPledge" />.</summary>
public readonly record struct FinancialPledgeId(Guid Value) : IStronglyTypedId<FinancialPledgeId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialPledgeId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialPledgeId Create()
    {
        return new FinancialPledgeId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialPledgeId From(Guid value)
    {
        return new FinancialPledgeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
