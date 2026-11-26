using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Touchpoint.</summary>
public readonly record struct TouchpointId(Guid Value) : IStronglyTypedId<TouchpointId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static TouchpointId Empty => new(Guid.Empty);
    public static TouchpointId Create() => new(StronglyTypedId.NextGuid());
    public static TouchpointId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
