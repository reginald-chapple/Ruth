using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a DataViewFilter node.</summary>
public readonly record struct DataViewFilterId(Guid Value) : IStronglyTypedId<DataViewFilterId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DataViewFilterId Empty => new(Guid.Empty);

    public static DataViewFilterId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static DataViewFilterId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
