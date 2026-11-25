using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Evaluates and persists all applicable GroupMemberRequirements for a GroupMember.</summary>
public sealed record EvaluateGroupMemberRequirementsCommand(Guid GroupMemberId)
    : ICommand<IReadOnlyList<GroupMemberRequirementResponse>>;
