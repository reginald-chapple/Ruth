using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MetricValue.</summary>
public readonly record struct MetricValueId(Guid Value) : IStronglyTypedId<MetricValueId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MetricValueId Empty => new(Guid.Empty);

    public static MetricValueId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MetricValueId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
