namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Binding of a GroupRequirementType to either a Group or a GroupType.</summary>
public sealed record GroupRequirementResponse(
    Guid Id,
    Guid? GroupId,
    Guid? GroupTypeId,
    Guid GroupRequirementTypeId,
    string RequirementTypeName,
    RequirementCheckType CheckType,
    Guid? GroupRoleId,
    bool MustMeetRequirementToAddMember,
    bool AllowLeadersToOverride,
    RequirementAgeClassification AppliesToAgeClassification,
    DateOnly? DueDateStaticDate,
    string? DueDateAttributeKey
);
