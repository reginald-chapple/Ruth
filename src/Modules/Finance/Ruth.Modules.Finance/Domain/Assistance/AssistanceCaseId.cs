using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AssistanceCase" />.</summary>
public readonly record struct AssistanceCaseId(Guid Value) : IStronglyTypedId<AssistanceCaseId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static AssistanceCaseId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static AssistanceCaseId Create()
    {
        return new AssistanceCaseId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static AssistanceCaseId From(Guid value)
    {
        return new AssistanceCaseId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
