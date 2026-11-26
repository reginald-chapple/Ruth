using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ContentChannelType.</summary>
public readonly record struct ContentChannelTypeId(Guid Value)
    : IStronglyTypedId<ContentChannelTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentChannelTypeId Empty => new(Guid.Empty);

    public static ContentChannelTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentChannelTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
