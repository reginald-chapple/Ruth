using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an InteractionSession.</summary>
public readonly record struct InteractionSessionId(Guid Value)
    : IStronglyTypedId<InteractionSessionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static InteractionSessionId Empty => new(Guid.Empty);

    public static InteractionSessionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static InteractionSessionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
