using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Achievements;

/// <summary>Event-driven badge, milestone, or certification achievement definition.</summary>
public sealed partial class AchievementType
    : AggregateRoot<AchievementTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.AchievementType";

    private List<Guid> _prerequisiteAchievementIds = [];

    private AchievementType() { }

    private AchievementType(AchievementTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public AchievementSourceKind SourceKind { get; private set; }
    public int TargetCount { get; private set; } = 1;
    public Guid? TargetStreakTypeId { get; private set; }
    public Guid? TargetJourneyProgramId { get; private set; }
    public Guid? TargetMilestoneTypeId { get; private set; }
    public bool AllowOverAchievement { get; private set; }
    public int? MaxSuccessesAllowed { get; private set; } = 1;
    public string? BadgeIconCssClass { get; private set; }
    public string? HighlightColorHex { get; private set; }
    public string? CelebrationSystemCommunicationKey { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyList<Guid> PrerequisiteAchievementIds
    {
        get => _prerequisiteAchievementIds;
        private set => _prerequisiteAchievementIds = [.. value];
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();
    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action) =>
        GlobalDefaultSecured.Instance.IsAllowedByDefault(action);

    public void SetCreated(DateTimeOffset createdOnUtc) =>
        CreatedOnUtc = createdOnUtc.ToUniversalTime();

    public void SetModified(DateTimeOffset modifiedOnUtc) =>
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();

    public void SetConcurrencyStamp(Guid newStamp) => ConcurrencyStamp = newStamp;

    public static Result<AchievementType> Create(AchievementTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new AchievementType(AchievementTypeId.Create());
        var res = entity.Update(spec);
        return res.IsFailure ? Result.Failure<AchievementType>(res.Errors) : entity;
    }

    public Result Update(AchievementTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var code = spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 200
            || !CodeRegex.IsMatch(code)
            || spec.TargetCount < 1
            || (spec.MaxSuccessesAllowed.HasValue && spec.MaxSuccessesAllowed.Value < 1)
        )
        {
            return EngagementErrors.AchievementTypeInvalid;
        }

        Name = spec.Name.Trim();
        Code = code;
        Description = TrimOptional(spec.Description, 1000);
        CategoryId = NormalizeGuid(spec.CategoryId);
        SourceKind = spec.SourceKind;
        TargetCount = spec.TargetCount;
        TargetStreakTypeId = NormalizeGuid(spec.TargetStreakTypeId);
        TargetJourneyProgramId = NormalizeGuid(spec.TargetJourneyProgramId);
        TargetMilestoneTypeId = NormalizeGuid(spec.TargetMilestoneTypeId);
        AllowOverAchievement = spec.AllowOverAchievement;
        MaxSuccessesAllowed = spec.MaxSuccessesAllowed;
        BadgeIconCssClass = TrimOptional(spec.BadgeIconCssClass, 100);
        HighlightColorHex = TrimOptional(spec.HighlightColorHex, 16);
        CelebrationSystemCommunicationKey = TrimOptional(
            spec.CelebrationSystemCommunicationKey,
            120
        );
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
        _prerequisiteAchievementIds =
            spec.PrerequisiteAchievementIds is null
                ? []
                :
                [
                    .. spec.PrerequisiteAchievementIds
                        .Where(id => id != Guid.Empty && id != Id.Value)
                        .Distinct(),
                ];

        return Result.Success();
    }

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CodeRegex { get; }
}
