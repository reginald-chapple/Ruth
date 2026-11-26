using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Query to list event calendars.</summary>
public sealed record GetEventCalendarsQuery(bool PublicOnly = false, bool ActiveOnly = false)
    : IQuery<IReadOnlyList<EventCalendarResponse>>;
