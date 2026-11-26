using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.Streaks;

/// <summary>Constituent's bitmask participation ledger and materialized streak metrics for a StreakType.</summary>
public sealed class Streak : AggregateRoot<StreakId>, IAuditableEntity, IHasConcurrencyToken
{
    private byte[] _engagementMap = [];

    private Streak() { }

    private Streak(StreakId id)
        : base(id) { }

    public Guid StreakTypeId { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateOnly EnrollmentDate { get; private set; }
    public DateOnly? InactiveDate { get; private set; }

    public byte[] EngagementMap
    {
        get => _engagementMap;
        private set => _engagementMap = [.. value];
    }

    public int EngagementCount { get; private set; }
    public int CurrentStreakCount { get; private set; }
    public DateOnly? CurrentStreakStartDate { get; private set; }
    public int LongestStreakCount { get; private set; }
    public DateOnly? LongestStreakStartDate { get; private set; }
    public DateOnly? LongestStreakEndDate { get; private set; }
    public DateOnly? MostRecentEngagementDate { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<Streak> Enroll(
        StreakType streakType,
        Guid personId,
        Guid? branchId,
        DateOnly enrollmentDate
    )
    {
        ArgumentNullException.ThrowIfNull(streakType);
        return personId == Guid.Empty
            ? EngagementErrors.StreakTypeInvalid
            : new Streak(StreakId.Create())
            {
                StreakTypeId = streakType.Id.Value,
                PersonId = personId,
                BranchId = NormalizeGuid(branchId),
                EnrollmentDate = enrollmentDate,
            };
    }

    public Result RecordEngagement(
        StreakType streakType,
        DateOnly engagementDate,
        DateOnly asOfDate,
        Guid? branchId = null
    )
    {
        ArgumentNullException.ThrowIfNull(streakType);
        var bucketIdx = StreakBitmaskEngine.ComputeBucketIndex(
            streakType.AnchorStartDate,
            engagementDate,
            streakType.Cadence,
            streakType.WeekEndDay
        );
        if (bucketIdx < 0)
        {
            return EngagementErrors.StreakDateBeforeAnchor;
        }

        BranchId ??= NormalizeGuid(branchId);
        _engagementMap = StreakBitmaskEngine.EnsureAndSetBit(_engagementMap, bucketIdx);
        var evalDate = asOfDate >= engagementDate ? asOfDate : engagementDate;
        RebuildMetrics(streakType, evalDate);
        return Result.Success();
    }

    public void RebuildMetrics(
        StreakType streakType,
        DateOnly asOfDate,
        bool allowCurrentBucketGrace = true
    )
    {
        ArgumentNullException.ThrowIfNull(streakType);
        var asOfBucket = Math.Max(
            StreakBitmaskEngine.FindHighestSetBitIndex(_engagementMap),
            StreakBitmaskEngine.ComputeBucketIndex(
                streakType.AnchorStartDate,
                asOfDate,
                streakType.Cadence,
                streakType.WeekEndDay
            )
        );
        if (asOfBucket < 0)
        {
            return;
        }

        var exclusionMap = StreakBitmaskEngine.BuildExclusionMap(
            streakType.AnchorStartDate,
            streakType.Cadence,
            streakType.WeekEndDay,
            streakType.Exclusions,
            BranchId,
            asOfBucket
        );

        var metrics = StreakBitmaskEngine.ComputeMetrics(
            _engagementMap,
            exclusionMap,
            streakType.AnchorStartDate,
            streakType.Cadence,
            streakType.WeekEndDay,
            asOfBucket,
            allowCurrentBucketGrace
        );

        EngagementCount = metrics.EngagementCount;
        CurrentStreakCount = metrics.CurrentStreakCount;
        CurrentStreakStartDate = metrics.CurrentStreakStartDate;
        LongestStreakCount = metrics.LongestStreakCount;
        LongestStreakStartDate = metrics.LongestStreakStartDate;
        LongestStreakEndDate = metrics.LongestStreakEndDate;
        MostRecentEngagementDate = metrics.MostRecentEngagementDate;
    }

    public void MergeFrom(Streak retired, StreakType streakType, DateOnly asOfDate)
    {
        ArgumentNullException.ThrowIfNull(retired);
        ArgumentNullException.ThrowIfNull(streakType);
        if (retired.EnrollmentDate < EnrollmentDate)
        {
            EnrollmentDate = retired.EnrollmentDate;
        }

        _engagementMap = StreakBitmaskEngine.MergeBitwiseOr(_engagementMap, retired._engagementMap);
        RebuildMetrics(streakType, asOfDate);
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
