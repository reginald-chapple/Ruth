using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a CommerceOrderLine.</summary>
public readonly record struct CommerceOrderLineId(Guid Value)
    : IStronglyTypedId<CommerceOrderLineId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static CommerceOrderLineId Empty => new(Guid.Empty);

    public static CommerceOrderLineId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static CommerceOrderLineId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
