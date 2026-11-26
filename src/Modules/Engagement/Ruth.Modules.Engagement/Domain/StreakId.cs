using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Streak.</summary>
public readonly record struct StreakId(Guid Value) : IStronglyTypedId<StreakId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static StreakId Empty => new(Guid.Empty);
    public static StreakId Create() => new(StronglyTypedId.NextGuid());
    public static StreakId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
