using Ruth.Modules.Core.Contracts.Campuses;

namespace Ruth.Modules.Core.Domain;

internal sealed record CampusInput(
    string Name,
    string? Description,
    string? ShortCode,
    bool IsActive,
    string? Url,
    string? PhoneNumber,
    string? TimeZoneId,
    LocationId? LocationId,
    Guid? LeaderPersonId,
    Guid? CampusStatusValueId,
    Guid? CampusTypeValueId,
    Guid? PhotoId,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    decimal? TitheMetric,
    int? AverageWeekendAttendance,
    int? BeaconId,
    int Order,
    IReadOnlyList<CampusScheduleDto>? Schedules,
    IReadOnlyList<CampusTopicDto>? Topics
);
