using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Creates a new branch or site with optional schedules and topics.</summary>
public sealed record CreateBranchCommand(
    string Name,
    string? Description = null,
    string? ShortCode = null,
    bool IsActive = true,
    string? Url = null,
    string? PhoneNumber = null,
    string? TimeZoneId = null,
    Guid? LocationId = null,
    Guid? LeaderPersonId = null,
    Guid? BranchStatusValueId = null,
    Guid? BranchTypeValueId = null,
    Guid? PhotoId = null,
    DateOnly? OpenedDate = null,
    DateOnly? ClosedDate = null,
    int? BeaconId = null,
    int Order = 0,
    IReadOnlyList<BranchScheduleDto>? Schedules = null,
    IReadOnlyList<BranchTopicDto>? Topics = null
) : ICommand<BranchResponse>;
