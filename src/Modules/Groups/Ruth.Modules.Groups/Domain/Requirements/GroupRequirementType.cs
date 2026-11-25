using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Requirements;

/// <summary>Reusable definition of a group membership prerequisite (Manual, BackgroundCheck, Attribute, or GroupMembership).</summary>
public sealed class GroupRequirementType
    : AggregateRoot<GroupRequirementTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type key for GroupRequirementType.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupRequirementType";

    private GroupRequirementType() { }

    private GroupRequirementType(GroupRequirementTypeId id)
        : base(id) { }

    /// <summary>Gets the requirement display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional detailed description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the short summary text.</summary>
    public string? Summary { get; private set; }

    /// <summary>Gets the check mechanism used to evaluate this requirement.</summary>
    public RequirementCheckType CheckType { get; private set; }

    /// <summary>Gets the structured rule parameter key (e.g., attribute key, package name, or prerequisite group ID).</summary>
    public string? RuleKey { get; private set; }

    /// <summary>Gets the structured rule parameter expected value.</summary>
    public string? RuleValue { get; private set; }

    /// <summary>Gets whether met status expires after <see cref="ExpireInDays" />.</summary>
    public bool CanExpire { get; private set; }

    /// <summary>Gets the expiration duration in days when <see cref="CanExpire" /> is true.</summary>
    public int? ExpireInDays { get; private set; }

    /// <summary>Gets optional warning threshold in days prior to expiration.</summary>
    public int? WarningDaysBeforeExpiration { get; private set; }

    /// <summary>Gets the positive status label.</summary>
    public string? PositiveLabel { get; private set; }

    /// <summary>Gets the negative status label.</summary>
    public string? NegativeLabel { get; private set; }

    /// <summary>Gets the warning status label.</summary>
    public string? WarningLabel { get; private set; }

    /// <summary>Gets the manual checkbox label.</summary>
    public string? CheckboxLabel { get; private set; }

    /// <summary>Gets the optional icon CSS class or icon identifier.</summary>
    public string? IconCssClass { get; private set; }

    /// <summary>Gets how due dates are calculated for members.</summary>
    public RequirementDueDateType DueDateType { get; private set; }

    /// <summary>Gets the optional offset in days used for due date computation.</summary>
    public int? DueDateOffsetInDays { get; private set; }

    /// <summary>Gets the optional Core Category identifier.</summary>
    public Guid? CategoryId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Resolves the human-readable label for an evaluated <see cref="MeetsGroupRequirement" /> state.</summary>
    public string ResolveLabel(MeetsGroupRequirement state)
    {
        return state switch
        {
            MeetsGroupRequirement.Meets => PositiveLabel ?? $"Meets {Name}",
            MeetsGroupRequirement.MeetsWithWarning => WarningLabel ?? $"{Name} Expiring Soon",
            MeetsGroupRequirement.NotApplicable => "Not Applicable",
            MeetsGroupRequirement.Error => $"{Name} Evaluation Error",
            MeetsGroupRequirement.NotMet or _ => NegativeLabel ?? $"Does Not Meet {Name}",
        };
    }

    internal static Result<GroupRequirementType> Create(
        string name,
        RequirementCheckType checkType,
        string? description,
        string? summary,
        string? ruleKey,
        string? ruleValue,
        bool canExpire,
        int? expireInDays,
        int? warningDaysBeforeExpiration,
        string? positiveLabel,
        string? negativeLabel,
        string? warningLabel,
        string? checkboxLabel,
        string? iconCssClass,
        RequirementDueDateType dueDateType,
        int? dueDateOffsetInDays,
        Guid? categoryId
    )
    {
        var nameResult = GroupsTextPolicy.NormalizeRequired(name);
        var descResult = GroupsTextPolicy.NormalizeOptional(description);
        var summaryResult = GroupsTextPolicy.NormalizeOptional(summary);
        var combined = Result.Combine(nameResult, descResult, summaryResult);
        return combined.IsFailure ? Result.Failure<GroupRequirementType>(combined.Errors)
            : expireInDays is <= 0 || warningDaysBeforeExpiration is < 0
                ? (Result<GroupRequirementType>)GroupsErrors.InvalidCapacity
            : Result.Success(
                new GroupRequirementType(GroupRequirementTypeId.Create())
                {
                    Name = nameResult.Value,
                    CheckType = checkType,
                    Description = descResult.Value,
                    Summary = summaryResult.Value,
                    RuleKey = ruleKey?.Trim(),
                    RuleValue = ruleValue?.Trim(),
                    CanExpire = canExpire,
                    ExpireInDays = canExpire ? expireInDays : null,
                    WarningDaysBeforeExpiration = canExpire ? warningDaysBeforeExpiration : null,
                    PositiveLabel = positiveLabel?.Trim(),
                    NegativeLabel = negativeLabel?.Trim(),
                    WarningLabel = warningLabel?.Trim(),
                    CheckboxLabel = checkboxLabel?.Trim(),
                    IconCssClass = iconCssClass?.Trim(),
                    DueDateType = dueDateType,
                    DueDateOffsetInDays = dueDateOffsetInDays,
                    CategoryId = categoryId,
                }
            );
    }
}
