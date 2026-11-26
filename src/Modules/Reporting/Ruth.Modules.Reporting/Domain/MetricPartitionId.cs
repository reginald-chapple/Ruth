using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MetricPartition.</summary>
public readonly record struct MetricPartitionId(Guid Value) : IStronglyTypedId<MetricPartitionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MetricPartitionId Empty => new(Guid.Empty);

    public static MetricPartitionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MetricPartitionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
