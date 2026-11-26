using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Touchpoint.</summary>
public readonly record struct TouchpointId(Guid Value) : IStronglyTypedId<TouchpointId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static TouchpointId Empty => new(Guid.Empty);

    public static TouchpointId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static TouchpointId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
