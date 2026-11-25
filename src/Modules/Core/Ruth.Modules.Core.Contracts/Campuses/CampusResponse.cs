namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Detailed representation of a campus including schedules and topics.</summary>
public sealed record CampusResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string CondensedName,
    string? Description,
    string? ShortCode,
    bool IsActive,
    bool IsSystem,
    string? Url,
    string? PhoneNumber,
    string? TimeZoneId,
    Guid? LocationId,
    Guid? LeaderPersonId,
    Guid? CampusStatusValueId,
    Guid? CampusTypeValueId,
    Guid? TeamGroupId,
    Guid? PhotoId,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    decimal? TitheMetric,
    int? AverageWeekendAttendance,
    int? BeaconId,
    int Order,
    IReadOnlyList<CampusScheduleDto> Schedules,
    IReadOnlyList<CampusTopicDto> Topics,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
