using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Query to compile an RFC 5545 iCalendar (.ics) feed for a calendar over a date horizon.</summary>
public sealed record GetCalendarIcsFeedQuery(
    string CalendarIdOrSlug,
    DateTimeOffset? RangeStartUtc = null,
    DateTimeOffset? RangeEndUtc = null,
    Guid? BranchId = null,
    Guid? AudienceDefinedValueId = null
) : IQuery<CalendarFeedPayloadResponse>;
