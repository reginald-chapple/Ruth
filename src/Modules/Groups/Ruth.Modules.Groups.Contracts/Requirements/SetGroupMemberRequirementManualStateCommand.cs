using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Marks a manual requirement complete or records a leader override for a GroupMemberRequirement.</summary>
public sealed record SetGroupMemberRequirementManualStateCommand(
    Guid GroupMemberId,
    Guid GroupRequirementId,
    bool IsOverride,
    bool IsMet,
    Guid? ActorPersonId = null,
    string? Note = null
) : ICommand<GroupMemberRequirementResponse>;
