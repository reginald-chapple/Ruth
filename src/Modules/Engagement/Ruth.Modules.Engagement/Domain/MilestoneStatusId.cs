using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MilestoneStatus.</summary>
public readonly record struct MilestoneStatusId(Guid Value) : IStronglyTypedId<MilestoneStatusId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MilestoneStatusId Empty => new(Guid.Empty);

    public static MilestoneStatusId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MilestoneStatusId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
