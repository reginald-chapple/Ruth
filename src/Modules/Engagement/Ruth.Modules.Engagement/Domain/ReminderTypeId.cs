using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ReminderType.</summary>
public readonly record struct ReminderTypeId(Guid Value) : IStronglyTypedId<ReminderTypeId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReminderTypeId Empty => new(Guid.Empty);

    public static ReminderTypeId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ReminderTypeId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
