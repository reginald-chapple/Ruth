using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Query to list event items filtered optionally by calendar, branch, audience, or approval state.</summary>
public sealed record GetEventItemsQuery(
    Guid? CalendarId = null,
    Guid? BranchId = null,
    Guid? AudienceDefinedValueId = null,
    bool ActiveOnly = false,
    bool PublicApprovedOnly = false
) : IQuery<IReadOnlyList<EventItemResponse>>;
