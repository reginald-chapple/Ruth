using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Lists evaluated GroupMemberRequirements for a GroupMember.</summary>
public sealed record GetGroupMemberRequirementsQuery(Guid GroupMemberId)
    : IQuery<IReadOnlyList<GroupMemberRequirementResponse>>;
