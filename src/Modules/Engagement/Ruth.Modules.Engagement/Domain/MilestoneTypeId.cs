using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MilestoneType.</summary>
public readonly record struct MilestoneTypeId(Guid Value) : IStronglyTypedId<MilestoneTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MilestoneTypeId Empty => new(Guid.Empty);
    public static MilestoneTypeId Create() => new(StronglyTypedId.NextGuid());
    public static MilestoneTypeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
