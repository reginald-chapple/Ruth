using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an EngagementRequestActivity.</summary>
public readonly record struct EngagementRequestActivityId(Guid Value)
    : IStronglyTypedId<EngagementRequestActivityId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static EngagementRequestActivityId Empty => new(Guid.Empty);
    public static EngagementRequestActivityId Create() => new(StronglyTypedId.NextGuid());
    public static EngagementRequestActivityId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
