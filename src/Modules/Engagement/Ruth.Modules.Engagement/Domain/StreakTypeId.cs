using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a StreakType.</summary>
public readonly record struct StreakTypeId(Guid Value) : IStronglyTypedId<StreakTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static StreakTypeId Empty => new(Guid.Empty);
    public static StreakTypeId Create() => new(StronglyTypedId.NextGuid());
    public static StreakTypeId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
