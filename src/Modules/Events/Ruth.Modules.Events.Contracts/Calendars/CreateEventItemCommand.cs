using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Command to create an event item with calendar memberships and audiences.</summary>
public sealed record CreateEventItemCommand(EventItemSpec Spec) : ICommand<EventItemResponse>;
