using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="EventItemAudience" />.</summary>
public readonly record struct EventItemAudienceId(Guid Value)
    : IStronglyTypedId<EventItemAudienceId>
{
    public bool IsEmpty => Value == Guid.Empty;

    public static EventItemAudienceId Empty => new(Guid.Empty);

    public static EventItemAudienceId Create()
    {
        return new EventItemAudienceId(StronglyTypedId.NextGuid());
    }

    public static EventItemAudienceId From(Guid value)
    {
        return new EventItemAudienceId(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
