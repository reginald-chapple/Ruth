using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Search.Domain;

public readonly record struct ContentCollectionSourceId(Guid Value)
    : IStronglyTypedId<ContentCollectionSourceId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentCollectionSourceId Empty => new(Guid.Empty);

    public static ContentCollectionSourceId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentCollectionSourceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
