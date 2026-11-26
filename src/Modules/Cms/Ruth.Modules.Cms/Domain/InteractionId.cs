using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an Interaction.</summary>
public readonly record struct InteractionId(Guid Value) : IStronglyTypedId<InteractionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static InteractionId Empty => new(Guid.Empty);

    public static InteractionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static InteractionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
