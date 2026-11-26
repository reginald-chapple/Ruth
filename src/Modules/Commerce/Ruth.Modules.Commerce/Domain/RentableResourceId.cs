using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a RentableResource.</summary>
public readonly record struct RentableResourceId(Guid Value) : IStronglyTypedId<RentableResourceId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static RentableResourceId Empty => new(Guid.Empty);

    public static RentableResourceId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static RentableResourceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
