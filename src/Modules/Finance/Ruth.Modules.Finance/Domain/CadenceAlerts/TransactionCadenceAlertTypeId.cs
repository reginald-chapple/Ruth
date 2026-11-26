using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="TransactionCadenceAlertType" />.</summary>
public readonly record struct TransactionCadenceAlertTypeId(Guid Value)
    : IStronglyTypedId<TransactionCadenceAlertTypeId>
{
    /// <inheritdoc />
    public static TransactionCadenceAlertTypeId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static TransactionCadenceAlertTypeId Create()
    {
        return new TransactionCadenceAlertTypeId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static TransactionCadenceAlertTypeId From(Guid value)
    {
        return new TransactionCadenceAlertTypeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
