using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ContentChannel.</summary>
public readonly record struct ContentChannelId(Guid Value) : IStronglyTypedId<ContentChannelId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentChannelId Empty => new(Guid.Empty);

    public static ContentChannelId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentChannelId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
