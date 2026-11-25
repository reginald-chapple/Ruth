namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Evaluated requirement status for a specific GroupMember.</summary>
public sealed record GroupMemberRequirementResponse(
    Guid Id,
    Guid GroupMemberId,
    Guid GroupId,
    Guid PersonId,
    Guid GroupRequirementId,
    Guid GroupRequirementTypeId,
    string RequirementTypeName,
    RequirementCheckType CheckType,
    MeetsGroupRequirement State,
    string DisplayLabel,
    DateTimeOffset? RequirementMetOnUtc,
    DateTimeOffset? RequirementFailOnUtc,
    DateTimeOffset? RequirementWarningOnUtc,
    DateTimeOffset? LastCheckedOnUtc,
    DateOnly? DueDate,
    bool WasManuallyCompleted,
    Guid? ManuallyCompletedByPersonId,
    DateTimeOffset? ManuallyCompletedOnUtc,
    bool WasOverridden,
    Guid? OverriddenByPersonId,
    DateTimeOffset? OverriddenOnUtc,
    string? OverrideNote
);
