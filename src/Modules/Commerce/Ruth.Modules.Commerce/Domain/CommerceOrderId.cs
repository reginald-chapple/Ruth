using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CommerceOrder.</summary>
public readonly record struct CommerceOrderId(Guid Value) : IStronglyTypedId<CommerceOrderId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CommerceOrderId Empty => new(Guid.Empty);

    public static CommerceOrderId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CommerceOrderId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
