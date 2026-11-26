using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a StreakType.</summary>
public readonly record struct StreakTypeId(Guid Value) : IStronglyTypedId<StreakTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static StreakTypeId Empty => new(Guid.Empty);

    public static StreakTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static StreakTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
