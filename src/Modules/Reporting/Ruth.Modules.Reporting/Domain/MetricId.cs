using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Metric.</summary>
public readonly record struct MetricId(Guid Value) : IStronglyTypedId<MetricId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MetricId Empty => new(Guid.Empty);

    public static MetricId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MetricId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
