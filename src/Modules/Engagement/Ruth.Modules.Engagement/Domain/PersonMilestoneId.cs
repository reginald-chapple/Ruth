using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PersonMilestone.</summary>
public readonly record struct PersonMilestoneId(Guid Value) : IStronglyTypedId<PersonMilestoneId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PersonMilestoneId Empty => new(Guid.Empty);
    public static PersonMilestoneId Create() => new(StronglyTypedId.NextGuid());
    public static PersonMilestoneId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
