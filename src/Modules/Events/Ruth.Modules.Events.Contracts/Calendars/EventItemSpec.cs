namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Specification for creating or updating an EventItem across one or more calendars.</summary>
public sealed record EventItemSpec(
    string Name,
    string Slug,
    IReadOnlyList<Guid> CalendarIds,
    string? Summary = null,
    string? Description = null,
    Guid? PhotoBinaryFileId = null,
    string? DetailsUrl = null,
    IReadOnlyList<EventItemAudienceDto>? Audiences = null,
    EventItemApprovalStatus ApprovalStatus = EventItemApprovalStatus.Approved,
    Guid? ApprovedByPersonId = null,
    bool IsActive = true,
    bool IsPublic = true,
    IReadOnlyDictionary<string, string?>? Attributes = null
);
