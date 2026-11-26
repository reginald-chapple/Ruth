using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ContentTopic.</summary>
public readonly record struct ContentTopicId(Guid Value) : IStronglyTypedId<ContentTopicId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentTopicId Empty => new(Guid.Empty);

    public static ContentTopicId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentTopicId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
