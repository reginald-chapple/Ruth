using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an InteractionComponent.</summary>
public readonly record struct InteractionComponentId(Guid Value)
    : IStronglyTypedId<InteractionComponentId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static InteractionComponentId Empty => new(Guid.Empty);

    public static InteractionComponentId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static InteractionComponentId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
