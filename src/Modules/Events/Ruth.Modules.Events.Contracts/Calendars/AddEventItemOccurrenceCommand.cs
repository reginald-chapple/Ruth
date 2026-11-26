using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to attach a Branch/Location/Schedule or explicit-time occurrence to an event item.</summary>
public sealed record AddEventItemOccurrenceCommand(Guid EventItemId, EventItemOccurrenceSpec Spec)
    : ICommand<EventItemOccurrenceResponse>;
