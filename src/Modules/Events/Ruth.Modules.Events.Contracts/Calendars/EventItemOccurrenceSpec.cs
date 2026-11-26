namespace Ruth.Modules.Events.Contracts.Calendars;

/// <summary>Specification for adding or updating an event item occurrence linked to a Branch, Location, and Schedule or explicit UTC window.</summary>
public sealed record EventItemOccurrenceSpec(
    Guid? BranchId = null,
    Guid? LocationId = null,
    string? LocationDescription = null,
    Guid? ScheduleId = null,
    DateTimeOffset? CustomStartUtc = null,
    DateTimeOffset? CustomEndUtc = null,
    string? TimeZoneId = null,
    Guid? ContactPersonId = null,
    string? ContactPhone = null,
    string? ContactEmail = null,
    string? OccurrenceNote = null,
    Guid? RegistrationInstanceId = null,
    bool IsCancelled = false
);
