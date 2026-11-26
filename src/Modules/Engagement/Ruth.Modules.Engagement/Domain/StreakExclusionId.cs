using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a StreakExclusion.</summary>
public readonly record struct StreakExclusionId(Guid Value) : IStronglyTypedId<StreakExclusionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static StreakExclusionId Empty => new(Guid.Empty);

    public static StreakExclusionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static StreakExclusionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
