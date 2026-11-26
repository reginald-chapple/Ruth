using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Search.Domain;

public readonly record struct ContentCollectionId(Guid Value)
    : IStronglyTypedId<ContentCollectionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentCollectionId Empty => new(Guid.Empty);

    public static ContentCollectionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentCollectionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
