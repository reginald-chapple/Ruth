using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Reminder.</summary>
public readonly record struct ReminderId(Guid Value) : IStronglyTypedId<ReminderId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReminderId Empty => new(Guid.Empty);

    public static ReminderId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ReminderId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
