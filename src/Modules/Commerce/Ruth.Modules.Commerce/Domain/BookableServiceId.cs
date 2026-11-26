using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a BookableService.</summary>
public readonly record struct BookableServiceId(Guid Value) : IStronglyTypedId<BookableServiceId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static BookableServiceId Empty => new(Guid.Empty);

    public static BookableServiceId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static BookableServiceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
