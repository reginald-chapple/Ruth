using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ContentChannelItem.</summary>
public readonly record struct ContentChannelItemId(Guid Value)
    : IStronglyTypedId<ContentChannelItemId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentChannelItemId Empty => new(Guid.Empty);

    public static ContentChannelItemId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentChannelItemId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
