using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Campuses;

/// <summary>Updates an existing campus and replaces its schedules and topics when supplied.</summary>
public sealed record UpdateCampusCommand(
    Guid CampusId,
    string Name,
    string? Description = null,
    string? ShortCode = null,
    bool IsActive = true,
    string? Url = null,
    string? PhoneNumber = null,
    string? TimeZoneId = null,
    Guid? LocationId = null,
    Guid? LeaderPersonId = null,
    Guid? CampusStatusValueId = null,
    Guid? CampusTypeValueId = null,
    Guid? PhotoId = null,
    DateOnly? OpenedDate = null,
    DateOnly? ClosedDate = null,
    decimal? TitheMetric = null,
    int? AverageWeekendAttendance = null,
    int? BeaconId = null,
    int Order = 0,
    IReadOnlyList<CampusScheduleDto>? Schedules = null,
    IReadOnlyList<CampusTopicDto>? Topics = null
) : ICommand<CampusResponse>;
