using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to update an event item.</summary>
public sealed record UpdateEventItemCommand(Guid EventItemId, EventItemSpec Spec)
    : ICommand<EventItemResponse>;
