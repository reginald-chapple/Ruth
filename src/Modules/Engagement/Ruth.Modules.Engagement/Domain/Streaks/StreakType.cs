using System.Text.RegularExpressions;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Streaks;

/// <summary>Cadence configuration and exclusion calendar for high-density bitmask streaks.</summary>
public sealed partial class StreakType
    : AggregateRoot<StreakTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Engagement.StreakType";

    private readonly List<StreakExclusion> _exclusions = [];

    private StreakType() { }

    private StreakType(StreakTypeId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public StreakCadence Cadence { get; private set; }
    public DayOfWeek WeekEndDay { get; private set; } = DayOfWeek.Sunday;
    public DateOnly AnchorStartDate { get; private set; }
    public StreakSourceKind SourceKind { get; private set; }
    public Guid? TargetEntityId { get; private set; }
    public bool RequiresExplicitEnrollment { get; private set; }
    public bool EnableAchievements { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public IReadOnlyList<StreakExclusion> Exclusions => _exclusions;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();
    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<StreakType> Create(StreakTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var type = new StreakType(StreakTypeId.Create());
        var res = type.Update(spec);
        return res.IsFailure ? Result.Failure<StreakType>(res.Errors) : type;
    }

    public Result Update(StreakTypeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var code = spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || spec.Name.Trim().Length > 200
            || !CodeRegex.IsMatch(code)
        )
        {
            return EngagementErrors.StreakTypeInvalid;
        }

        Name = spec.Name.Trim();
        Code = code;
        Description = TrimOptional(spec.Description, 1000);
        Cadence = spec.Cadence;
        WeekEndDay = spec.WeekEndDay;
        AnchorStartDate = StreakBitmaskEngine.AlignToBucketDate(
            spec.AnchorStartDate,
            spec.Cadence,
            spec.WeekEndDay
        );
        SourceKind = spec.SourceKind;
        TargetEntityId = NormalizeGuid(spec.TargetEntityId);
        RequiresExplicitEnrollment = spec.RequiresExplicitEnrollment;
        EnableAchievements = spec.EnableAchievements;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    public Result AddExclusion(DateOnly exclusionDate, Guid? branchId)
    {
        var aligned = StreakBitmaskEngine.AlignToBucketDate(exclusionDate, Cadence, WeekEndDay);
        if (aligned < AnchorStartDate)
        {
            return EngagementErrors.StreakDateBeforeAnchor;
        }

        var normalizedBranch = NormalizeGuid(branchId);
        if (_exclusions.Exists(e => e.ExclusionDate == aligned && e.BranchId == normalizedBranch))
        {
            return Result.Success();
        }

        _exclusions.Add(
            new StreakExclusion(StreakExclusionId.Create(), Id, aligned, normalizedBranch)
        );
        return Result.Success();
    }

    public bool MatchesAttendance(Guid? groupId, Guid? groupTypeId)
    {
        return IsActive && MatchesSourceTarget(groupId, groupTypeId);
    }

    private bool MatchesSourceTarget(Guid? groupId, Guid? groupTypeId)
    {
        return SourceKind switch
        {
            StreakSourceKind.AnyAttendance => true,
            StreakSourceKind.GroupAttendance => MatchesTargetGuid(groupId),
            StreakSourceKind.GroupTypeAttendance => MatchesTargetGuid(groupTypeId),
            StreakSourceKind.ManualOrActivity => false,
            _ => false,
        };
    }

    private bool MatchesTargetGuid(Guid? candidate)
    {
        return TargetEntityId.HasValue && candidate == TargetEntityId;
    }

    [GeneratedRegex(
        @"^[A-Z0-9][A-Z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CodeRegex { get; }
}
