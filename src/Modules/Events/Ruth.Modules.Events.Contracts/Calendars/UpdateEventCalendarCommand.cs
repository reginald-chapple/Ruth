using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to update an existing event calendar.</summary>
public sealed record UpdateEventCalendarCommand(Guid CalendarId, EventCalendarSpec Spec)
    : ICommand<EventCalendarResponse>;
