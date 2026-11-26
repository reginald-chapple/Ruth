using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AssistanceDisbursement" />.</summary>
public readonly record struct AssistanceDisbursementId(Guid Value)
    : IStronglyTypedId<AssistanceDisbursementId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static AssistanceDisbursementId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static AssistanceDisbursementId Create()
    {
        return new AssistanceDisbursementId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static AssistanceDisbursementId From(Guid value)
    {
        return new AssistanceDisbursementId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
