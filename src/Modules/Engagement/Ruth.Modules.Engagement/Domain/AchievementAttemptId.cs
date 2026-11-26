using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an AchievementAttempt.</summary>
public readonly record struct AchievementAttemptId(Guid Value)
    : IStronglyTypedId<AchievementAttemptId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AchievementAttemptId Empty => new(Guid.Empty);
    public static AchievementAttemptId Create() => new(StronglyTypedId.NextGuid());
    public static AchievementAttemptId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
