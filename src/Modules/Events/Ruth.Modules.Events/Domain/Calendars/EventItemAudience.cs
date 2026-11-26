using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Audience tag attached to an <see cref="EventItem" />.</summary>
public sealed class EventItemAudience : Entity<EventItemAudienceId>
{
    private EventItemAudience() { }

    internal EventItemAudience(
        EventItemAudienceId id,
        EventItemId eventItemId,
        Guid? audienceDefinedValueId,
        string audienceLabel
    )
        : base(id)
    {
        EventItemId = eventItemId;
        AudienceDefinedValueId = audienceDefinedValueId is { } dv && dv != Guid.Empty ? dv : null;
        AudienceLabel = audienceLabel;
    }

    public EventItemId EventItemId { get; private set; }
    public Guid? AudienceDefinedValueId { get; private set; }
    public string AudienceLabel { get; private set; } = string.Empty;
}
