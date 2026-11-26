using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain.Achievements;

/// <summary>Constituent's attempt and earned progression toward an AchievementType.</summary>
public sealed class AchievementAttempt
    : AggregateRoot<AchievementAttemptId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private AchievementAttempt() { }

    private AchievementAttempt(AchievementAttemptId id)
        : base(id) { }

    public Guid AchievementTypeId { get; private set; }
    public Guid PersonId { get; private set; }
    public DateOnly AttemptStartDate { get; private set; }
    public DateOnly? AttemptEndDate { get; private set; }
    public int CurrentCount { get; private set; }
    public int TargetCountSnapshot { get; private set; }
    public decimal Progress { get; private set; }
    public bool IsClosed { get; private set; }
    public bool IsSuccessful { get; private set; }

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

    public static AchievementAttempt StartNew(
        AchievementType achievementType,
        Guid personId,
        DateOnly startDate
    )
    {
        ArgumentNullException.ThrowIfNull(achievementType);
        return new AchievementAttempt(AchievementAttemptId.Create())
        {
            AchievementTypeId = achievementType.Id.Value,
            PersonId = personId,
            AttemptStartDate = startDate,
            TargetCountSnapshot = achievementType.TargetCount,
        };
    }

    /// <summary>Applies an observed count and returns true if this call newly transitioned the attempt to successful.</summary>
    public bool ApplyProgress(AchievementType achievementType, int observedCount, DateOnly asOfDate)
    {
        ArgumentNullException.ThrowIfNull(achievementType);
        var sanitizedCount = Math.Max(0, observedCount);
        CurrentCount = Math.Max(CurrentCount, sanitizedCount);
        TargetCountSnapshot = achievementType.TargetCount;

        var rawRatio = decimal.Round(
            (decimal)CurrentCount / TargetCountSnapshot,
            4,
            MidpointRounding.AwayFromZero
        );
        Progress = achievementType.AllowOverAchievement ? rawRatio : Math.Min(1.0m, rawRatio);

        var newlySucceeded = !IsSuccessful && CurrentCount >= TargetCountSnapshot;
        if (CurrentCount >= TargetCountSnapshot)
        {
            IsSuccessful = true;
            AttemptEndDate ??= asOfDate;
            if (!achievementType.AllowOverAchievement)
            {
                IsClosed = true;
            }
        }

        return newlySucceeded;
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
