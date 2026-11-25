using Ruth.Modules.Core.Contracts.Branches;

namespace Ruth.Modules.Core.Domain;

internal sealed record BranchInput(
    string Name,
    string? Description,
    string? ShortCode,
    bool IsActive,
    string? Url,
    string? PhoneNumber,
    string? TimeZoneId,
    LocationId? LocationId,
    Guid? LeaderPersonId,
    Guid? BranchStatusValueId,
    Guid? BranchTypeValueId,
    Guid? PhotoId,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    int? BeaconId,
    int Order,
    IReadOnlyList<BranchScheduleDto>? Schedules,
    IReadOnlyList<BranchTopicDto>? Topics
);
