using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to update an existing event item occurrence.</summary>
public sealed record UpdateEventItemOccurrenceCommand(
    Guid EventItemId,
    Guid OccurrenceId,
    EventItemOccurrenceSpec Spec
) : ICommand<EventItemOccurrenceResponse>;
