using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Calendars;

/// <summary>Concrete or recurring schedule occurrence of an <see cref="EventItem" /> at an optional Branch and Location.</summary>
public sealed class EventItemOccurrence : Entity<EventItemOccurrenceId>
{
    private EventItemOccurrence() { }

    internal EventItemOccurrence(EventItemOccurrenceId id, EventItemId eventItemId)
        : base(id)
    {
        EventItemId = eventItemId;
    }

    public EventItemId EventItemId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? LocationId { get; private set; }
    public string? LocationDescription { get; private set; }
    public Guid? ScheduleId { get; private set; }
    public DateTimeOffset? CustomStartUtc { get; private set; }
    public DateTimeOffset? CustomEndUtc { get; private set; }
    public string? TimeZoneId { get; private set; }
    public Guid? ContactPersonId { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? OccurrenceNote { get; private set; }
    public Guid? RegistrationInstanceId { get; private set; }
    public bool IsCancelled { get; private set; }

    internal Result ApplySpec(EventItemOccurrenceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var scheduleId = NormalizeGuid(spec.ScheduleId);
        var hasCustomStart = spec.CustomStartUtc.HasValue;
        var validWindow =
            !spec.CustomEndUtc.HasValue
            || (
                spec.CustomStartUtc.HasValue && spec.CustomEndUtc.Value >= spec.CustomStartUtc.Value
            );

        if ((scheduleId is null && !hasCustomStart) || !validWindow)
        {
            return EventsErrors.OccurrenceInvalid;
        }

        BranchId = NormalizeGuid(spec.BranchId);
        LocationId = NormalizeGuid(spec.LocationId);
        LocationDescription = TrimOptional(spec.LocationDescription, 240);
        ScheduleId = scheduleId;
        CustomStartUtc = spec.CustomStartUtc?.ToUniversalTime();
        CustomEndUtc = spec.CustomEndUtc?.ToUniversalTime();
        TimeZoneId = TrimOptional(spec.TimeZoneId, 80);
        ContactPersonId = NormalizeGuid(spec.ContactPersonId);
        ContactPhone = TrimOptional(spec.ContactPhone, 40);
        ContactEmail = TrimOptional(spec.ContactEmail, 254);
        OccurrenceNote = TrimOptional(spec.OccurrenceNote, 1000);
        RegistrationInstanceId = NormalizeGuid(spec.RegistrationInstanceId);
        IsCancelled = spec.IsCancelled;
        return Result.Success();
    }

    public bool RepointContactPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ContactPersonId != retiredPersonId)
        {
            return false;
        }

        ContactPersonId = survivingPersonId;
        return true;
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
