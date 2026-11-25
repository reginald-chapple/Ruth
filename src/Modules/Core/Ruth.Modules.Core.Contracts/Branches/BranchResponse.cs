namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Detailed representation of an organization branch or site including schedules and contact topics.</summary>
public sealed record BranchResponse(
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
    Guid? BranchStatusValueId,
    Guid? BranchTypeValueId,
    Guid? TeamGroupId,
    Guid? PhotoId,
    DateOnly? OpenedDate,
    DateOnly? ClosedDate,
    int? BeaconId,
    int Order,
    IReadOnlyList<BranchScheduleDto> Schedules,
    IReadOnlyList<BranchTopicDto> Topics,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
