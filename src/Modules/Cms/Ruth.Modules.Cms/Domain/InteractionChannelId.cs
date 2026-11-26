using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an InteractionChannel.</summary>
public readonly record struct InteractionChannelId(Guid Value)
    : IStronglyTypedId<InteractionChannelId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static InteractionChannelId Empty => new(Guid.Empty);

    public static InteractionChannelId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static InteractionChannelId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
