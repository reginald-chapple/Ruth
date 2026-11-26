using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Domain.Achievements;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal static class AchievementProjectionMapper
{
    public static AchievementTypeResponse ToResponse(AchievementType entity)
    {
        return new AchievementTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.Code,
            entity.Description,
            entity.CategoryId,
            entity.SourceKind,
            entity.TargetCount,
            entity.TargetStreakTypeId,
            entity.TargetJourneyProgramId,
            entity.TargetMilestoneTypeId,
            entity.AllowOverAchievement,
            entity.MaxSuccessesAllowed,
            [.. entity.PrerequisiteAchievementIds],
            entity.BadgeIconCssClass,
            entity.HighlightColorHex,
            entity.CelebrationSystemCommunicationKey,
            entity.IsActive,
            entity.IsPublic
        );
    }

    public static AchievementAttemptResponse ToResponse(
        AchievementAttempt attempt,
        AchievementType achievementType
    )
    {
        return new AchievementAttemptResponse(
            attempt.Id.Value,
            attempt.AchievementTypeId,
            achievementType.Name,
            achievementType.Code,
            attempt.PersonId,
            attempt.AttemptStartDate,
            attempt.AttemptEndDate,
            attempt.CurrentCount,
            attempt.TargetCountSnapshot,
            attempt.Progress,
            attempt.IsClosed,
            attempt.IsSuccessful
        );
    }
}
