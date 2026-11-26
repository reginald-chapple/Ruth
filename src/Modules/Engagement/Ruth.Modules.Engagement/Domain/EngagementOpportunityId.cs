using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an EngagementOpportunity.</summary>
public readonly record struct EngagementOpportunityId(Guid Value)
    : IStronglyTypedId<EngagementOpportunityId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static EngagementOpportunityId Empty => new(Guid.Empty);
    public static EngagementOpportunityId Create() => new(StronglyTypedId.NextGuid());
    public static EngagementOpportunityId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
