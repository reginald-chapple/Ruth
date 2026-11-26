using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a DataView.</summary>
public readonly record struct DataViewId(Guid Value) : IStronglyTypedId<DataViewId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static DataViewId Empty => new(Guid.Empty);

    public static DataViewId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static DataViewId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
