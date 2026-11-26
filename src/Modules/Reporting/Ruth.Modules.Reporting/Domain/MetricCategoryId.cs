using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MetricCategory.</summary>
public readonly record struct MetricCategoryId(Guid Value) : IStronglyTypedId<MetricCategoryId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MetricCategoryId Empty => new(Guid.Empty);

    public static MetricCategoryId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MetricCategoryId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
