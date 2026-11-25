namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Definition of a reusable GroupRequirementType.</summary>
public sealed record GroupRequirementTypeResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Summary,
    RequirementCheckType CheckType,
    string? RuleKey,
    string? RuleValue,
    bool CanExpire,
    int? ExpireInDays,
    int? WarningDaysBeforeExpiration,
    string? PositiveLabel,
    string? NegativeLabel,
    string? WarningLabel,
    string? CheckboxLabel,
    string? IconCssClass,
    RequirementDueDateType DueDateType,
    int? DueDateOffsetInDays,
    Guid? CategoryId
);
