using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="TransactionCadenceAlert" />.</summary>
public readonly record struct TransactionCadenceAlertId(Guid Value)
    : IStronglyTypedId<TransactionCadenceAlertId>
{
    /// <inheritdoc />
    public static TransactionCadenceAlertId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static TransactionCadenceAlertId Create()
    {
        return new TransactionCadenceAlertId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static TransactionCadenceAlertId From(Guid value)
    {
        return new TransactionCadenceAlertId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
