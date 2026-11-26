using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ProductVariant.</summary>
public readonly record struct ProductVariantId(Guid Value) : IStronglyTypedId<ProductVariantId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ProductVariantId Empty => new(Guid.Empty);

    public static ProductVariantId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ProductVariantId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
