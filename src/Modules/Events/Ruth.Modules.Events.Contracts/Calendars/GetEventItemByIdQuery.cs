using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Query to retrieve a single event item by identifier.</summary>
public sealed record GetEventItemByIdQuery(Guid EventItemId) : IQuery<EventItemResponse>;
