using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AssistanceDocument" />.</summary>
public readonly record struct AssistanceDocumentId(Guid Value)
    : IStronglyTypedId<AssistanceDocumentId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static AssistanceDocumentId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static AssistanceDocumentId Create()
    {
        return new AssistanceDocumentId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static AssistanceDocumentId From(Guid value)
    {
        return new AssistanceDocumentId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
