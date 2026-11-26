using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PersistedDataset.</summary>
public readonly record struct PersistedDatasetId(Guid Value) : IStronglyTypedId<PersistedDatasetId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PersistedDatasetId Empty => new(Guid.Empty);

    public static PersistedDatasetId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PersistedDatasetId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
