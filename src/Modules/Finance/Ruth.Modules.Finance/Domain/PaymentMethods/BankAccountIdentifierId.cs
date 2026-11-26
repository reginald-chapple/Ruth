using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="BankAccountIdentifier" />.</summary>
public readonly record struct BankAccountIdentifierId(Guid Value)
    : IStronglyTypedId<BankAccountIdentifierId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static BankAccountIdentifierId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static BankAccountIdentifierId Create()
    {
        return new BankAccountIdentifierId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static BankAccountIdentifierId From(Guid value)
    {
        return new BankAccountIdentifierId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
