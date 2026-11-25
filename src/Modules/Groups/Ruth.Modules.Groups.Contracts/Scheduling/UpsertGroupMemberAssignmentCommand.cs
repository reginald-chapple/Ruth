using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Assigns a GroupMember's standing location/schedule preference.</summary>
public sealed record UpsertGroupMemberAssignmentCommand(
    Guid GroupMemberId,
    Guid? LocationId = null,
    Guid? ScheduleId = null
) : ICommand<GroupMemberAssignmentResponse>;
