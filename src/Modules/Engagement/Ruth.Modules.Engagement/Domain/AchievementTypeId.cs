using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an AchievementType.</summary>
public readonly record struct AchievementTypeId(Guid Value) : IStronglyTypedId<AchievementTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AchievementTypeId Empty => new(Guid.Empty);

    public static AchievementTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static AchievementTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
