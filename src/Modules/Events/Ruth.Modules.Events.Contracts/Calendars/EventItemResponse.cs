namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Complete read projection for an event item including calendar memberships, audiences, and occurrences.</summary>
public sealed record EventItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Summary,
    string? Description,
    Guid? PhotoBinaryFileId,
    string? DetailsUrl,
    EventItemApprovalStatus ApprovalStatus,
    Guid? ApprovedByPersonId,
    DateTimeOffset? ApprovedOnUtc,
    bool IsActive,
    bool IsPublic,
    IReadOnlyList<Guid> CalendarIds,
    IReadOnlyList<EventItemAudienceDto> Audiences,
    IReadOnlyList<EventItemOccurrenceResponse> Occurrences,
    IReadOnlyDictionary<string, string?> Attributes,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
