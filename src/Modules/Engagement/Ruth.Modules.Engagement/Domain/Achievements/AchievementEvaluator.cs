using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain.Achievements;

/// <summary>Clean-room eligibility and attempt progression evaluator for AchievementTypes.</summary>
public static class AchievementEvaluator
{
    /// <summary>Evaluates prerequisites and maximum success caps before starting or updating an attempt.</summary>
    public static Result<(AchievementAttempt Attempt, bool NewlyEarned)> EvaluateProgress(
        AchievementType achievementType,
        Guid personId,
        int observedCount,
        DateOnly asOfDate,
        IReadOnlyList<AchievementAttempt> existingAttemptsForType,
        IReadOnlySet<Guid> earnedAchievementTypeIds
    )
    {
        ArgumentNullException.ThrowIfNull(achievementType);
        ArgumentNullException.ThrowIfNull(existingAttemptsForType);
        ArgumentNullException.ThrowIfNull(earnedAchievementTypeIds);

        if (personId == Guid.Empty || observedCount < 0)
        {
            return EngagementErrors.AchievementTypeInvalid;
        }

        if (!achievementType.PrerequisiteAchievementIds.All(earnedAchievementTypeIds.Contains))
        {
            return EngagementErrors.AchievementPrerequisiteOrCapBlocked;
        }

        var openAttempt = existingAttemptsForType.FirstOrDefault(static a => !a.IsClosed);
        if (openAttempt is not null)
        {
            var newlyEarned = openAttempt.ApplyProgress(achievementType, observedCount, asOfDate);
            return (openAttempt, newlyEarned);
        }

        var successCount = existingAttemptsForType.Count(static a => a.IsSuccessful);
        if (
            achievementType.MaxSuccessesAllowed.HasValue
            && successCount >= achievementType.MaxSuccessesAllowed.Value
        )
        {
            return EngagementErrors.AchievementPrerequisiteOrCapBlocked;
        }

        var fresh = AchievementAttempt.StartNew(achievementType, personId, asOfDate);
        var earnedNow = fresh.ApplyProgress(achievementType, observedCount, asOfDate);
        return (fresh, earnedNow);
    }
}
