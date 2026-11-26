using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AssistanceProgram" />.</summary>
public readonly record struct AssistanceProgramId(Guid Value)
    : IStronglyTypedId<AssistanceProgramId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static AssistanceProgramId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static AssistanceProgramId Create()
    {
        return new AssistanceProgramId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static AssistanceProgramId From(Guid value)
    {
        return new AssistanceProgramId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
