using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MilestoneType.</summary>
public readonly record struct MilestoneTypeId(Guid Value) : IStronglyTypedId<MilestoneTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MilestoneTypeId Empty => new(Guid.Empty);

    public static MilestoneTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MilestoneTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
