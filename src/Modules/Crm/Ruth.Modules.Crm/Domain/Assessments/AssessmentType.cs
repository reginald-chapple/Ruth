using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Crm.Domain.Assessments;

/// <summary>Configures an assessment catalog entry (Team Strengths, DISC, Conflict, Motivators, EQ).</summary>
public sealed class AssessmentType
    : AggregateRoot<AssessmentTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for AssessmentType.</summary>
    public const string EntityTypeKeyName = "Ruth.Crm.AssessmentType";

    private AssessmentType() { }

    private AssessmentType(
        AssessmentTypeId id,
        string key,
        string title,
        string? description,
        string assessmentPath,
        string? assessmentResultsPath,
        bool requiresRequest,
        int minimumDaysToRetake,
        int validDurationDays,
        bool isSystem,
        string? iconName,
        string? badgeColor
    )
        : base(id)
    {
        Key = key;
        NormalizedKey = key.ToUpperInvariant();
        Title = title;
        Description = description;
        AssessmentPath = assessmentPath;
        AssessmentResultsPath = assessmentResultsPath;
        IsActive = true;
        RequiresRequest = requiresRequest;
        MinimumDaysToRetake = Math.Max(0, minimumDaysToRetake);
        ValidDurationDays = Math.Max(0, validDurationDays);
        IsSystem = isSystem;
        IconName = iconName;
        BadgeColor = badgeColor;
    }

    /// <summary>Gets the unique slug key (e.g. "disc", "team-strengths").</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized key.</summary>
    public string NormalizedKey { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable assessment title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the route/path where a person takes this assessment.</summary>
    public string AssessmentPath { get; private set; } = string.Empty;

    /// <summary>Gets the optional route/path displaying detailed results.</summary>
    public string? AssessmentResultsPath { get; private set; }

    /// <summary>Gets whether this assessment type is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether an explicit requester is required (disallowing self-initiation).</summary>
    public bool RequiresRequest { get; private set; }

    /// <summary>Gets the minimum cooldown in days before retaking (0 = no cooldown).</summary>
    public int MinimumDaysToRetake { get; private set; }

    /// <summary>Gets the validity duration in days (0 = indefinite).</summary>
    public int ValidDurationDays { get; private set; }

    /// <summary>Gets whether this assessment type is system-defined.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the optional icon name.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the optional hex badge color.</summary>
    public string? BadgeColor { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated assessment type.</summary>
    public static Result<AssessmentType> Create(
        string key,
        string title,
        string assessmentPath,
        string? description = null,
        string? assessmentResultsPath = null,
        bool requiresRequest = false,
        int minimumDaysToRetake = 0,
        int validDurationDays = 0,
        bool isSystem = false,
        string? iconName = null,
        string? badgeColor = null,
        AssessmentTypeId? explicitId = null
    )
    {
        if (
            string.IsNullOrWhiteSpace(key)
            || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(assessmentPath)
        )
        {
            return CrmErrors.AssessmentTypeInvalid;
        }

        var desc = CrmTextPolicy.ValidateOptionalText(
            description,
            CrmTextPolicy.DescriptionMaxLength
        );
        return desc.IsFailure
            ? Result.Failure<AssessmentType>(desc.Errors)
            : Result.Success(
                new AssessmentType(
                    explicitId ?? AssessmentTypeId.Create(),
                    key.Trim().ToLowerInvariant(),
                    title.Trim(),
                    desc.Value,
                    assessmentPath.Trim(),
                    string.IsNullOrWhiteSpace(assessmentResultsPath)
                        ? null
                        : assessmentResultsPath.Trim(),
                    requiresRequest,
                    minimumDaysToRetake,
                    validDurationDays,
                    isSystem,
                    string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
                    string.IsNullOrWhiteSpace(badgeColor) ? null : badgeColor.Trim()
                )
            );
    }

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
}
