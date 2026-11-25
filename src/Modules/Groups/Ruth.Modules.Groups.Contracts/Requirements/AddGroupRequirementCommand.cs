using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Attaches a GroupRequirement to either a Group or a GroupType.</summary>
public sealed record AddGroupRequirementCommand(
    Guid GroupRequirementTypeId,
    Guid? GroupId = null,
    Guid? GroupTypeId = null,
    Guid? GroupRoleId = null,
    bool MustMeetRequirementToAddMember = false,
    bool AllowLeadersToOverride = false,
    RequirementAgeClassification AppliesToAgeClassification = RequirementAgeClassification.All,
    DateOnly? DueDateStaticDate = null,
    string? DueDateAttributeKey = null
) : ICommand<GroupRequirementResponse>;
