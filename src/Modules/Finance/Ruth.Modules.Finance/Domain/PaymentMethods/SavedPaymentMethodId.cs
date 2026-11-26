using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SavedPaymentMethod" />.</summary>
public readonly record struct SavedPaymentMethodId(Guid Value)
    : IStronglyTypedId<SavedPaymentMethodId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static SavedPaymentMethodId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static SavedPaymentMethodId Create()
    {
        return new SavedPaymentMethodId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static SavedPaymentMethodId From(Guid value)
    {
        return new SavedPaymentMethodId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
