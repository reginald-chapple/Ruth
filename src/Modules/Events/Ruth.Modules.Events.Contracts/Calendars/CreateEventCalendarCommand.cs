using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to create a new event calendar.</summary>
public sealed record CreateEventCalendarCommand(EventCalendarSpec Spec)
    : ICommand<EventCalendarResponse>;
