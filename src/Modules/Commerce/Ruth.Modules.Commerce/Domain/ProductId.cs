using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Product.</summary>
public readonly record struct ProductId(Guid Value) : IStronglyTypedId<ProductId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ProductId Empty => new(Guid.Empty);

    public static ProductId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ProductId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
