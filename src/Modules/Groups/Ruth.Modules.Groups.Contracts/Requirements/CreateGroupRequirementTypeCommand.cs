using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Requirements;

/// <summary>Creates a reusable GroupRequirementType.</summary>
public sealed record CreateGroupRequirementTypeCommand(
    string Name,
    RequirementCheckType CheckType,
    string? Description = null,
    string? Summary = null,
    string? RuleKey = null,
    string? RuleValue = null,
    bool CanExpire = false,
    int? ExpireInDays = null,
    int? WarningDaysBeforeExpiration = null,
    string? PositiveLabel = null,
    string? NegativeLabel = null,
    string? WarningLabel = null,
    string? CheckboxLabel = null,
    string? IconCssClass = null,
    RequirementDueDateType DueDateType = RequirementDueDateType.Immediate,
    int? DueDateOffsetInDays = null,
    Guid? CategoryId = null
) : ICommand<GroupRequirementTypeResponse>;
