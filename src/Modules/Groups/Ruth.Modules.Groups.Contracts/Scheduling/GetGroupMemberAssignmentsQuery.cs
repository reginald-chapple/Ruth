using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Lists standing GroupMemberAssignments for a Group.</summary>
public sealed record GetGroupMemberAssignmentsQuery(Guid GroupId)
    : IQuery<IReadOnlyList<GroupMemberAssignmentResponse>>;
