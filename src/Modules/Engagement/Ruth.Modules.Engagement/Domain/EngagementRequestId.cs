using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an EngagementRequest.</summary>
public readonly record struct EngagementRequestId(Guid Value)
    : IStronglyTypedId<EngagementRequestId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static EngagementRequestId Empty => new(Guid.Empty);

    public static EngagementRequestId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static EngagementRequestId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
