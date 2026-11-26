namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Read projection for an event item occurrence.</summary>
public sealed record EventItemOccurrenceResponse(
    Guid Id,
    Guid EventItemId,
    Guid? BranchId,
    Guid? LocationId,
    string? LocationDescription,
    Guid? ScheduleId,
    DateTimeOffset? CustomStartUtc,
    DateTimeOffset? CustomEndUtc,
    string? TimeZoneId,
    Guid? ContactPersonId,
    string? ContactPhone,
    string? ContactEmail,
    string? OccurrenceNote,
    Guid? RegistrationInstanceId,
    bool IsCancelled
);
