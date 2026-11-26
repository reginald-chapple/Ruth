using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an EngagementRequestActivity.</summary>
public readonly record struct EngagementRequestActivityId(Guid Value)
    : IStronglyTypedId<EngagementRequestActivityId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static EngagementRequestActivityId Empty => new(Guid.Empty);

    public static EngagementRequestActivityId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static EngagementRequestActivityId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
