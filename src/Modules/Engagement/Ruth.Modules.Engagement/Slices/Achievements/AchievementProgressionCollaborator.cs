using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Domain.Achievements;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class AchievementProgressionCollaborator(
    EngagementDbContext dbContext,
    IClock clock
)
{
    public async Task<Result<(AchievementAttempt Attempt, AchievementType Type)>> ApplySingleAsync(
        AchievementType achievementType,
        Guid personId,
        int observedCount,
        DateOnly asOfDate,
        CancellationToken cancellationToken
    )
    {
        var allPersonAttempts = await dbContext
            .AchievementAttempts.Where(a => a.PersonId == personId)
            .ToListAsync(cancellationToken);

        var earnedIds = allPersonAttempts
            .Where(static a => a.IsSuccessful)
            .Select(static a => a.AchievementTypeId)
            .ToHashSet();

        var forType = allPersonAttempts
            .Where(a => a.AchievementTypeId == achievementType.Id.Value)
            .ToArray();

        var eval = AchievementEvaluator.EvaluateProgress(
            achievementType,
            personId,
            observedCount,
            asOfDate,
            forType,
            earnedIds
        );
        if (eval.IsFailure)
        {
            return Result.Failure<(AchievementAttempt, AchievementType)>(eval.Errors);
        }

        var (attempt, newlyEarned) = eval.Value;
        if (forType.All(a => a.Id != attempt.Id))
        {
            _ = dbContext.AchievementAttempts.Add(attempt);
        }

        if (newlyEarned)
        {
            EnqueueEarnedEvents(attempt, achievementType, asOfDate);
        }

        return (attempt, achievementType);
    }

    public async Task EvaluateStreakAchievementsAsync(
        Guid streakTypeId,
        Guid personId,
        int currentOrLongestStreak,
        int totalEngagements,
        DateOnly asOfDate,
        CancellationToken cancellationToken
    )
    {
        var candidates = await dbContext
            .AchievementTypes.AsNoTracking()
            .Where(t =>
                t.IsActive
                && (
                    (
                        t.SourceKind == AchievementSourceKind.StreakCount
                        && (!t.TargetStreakTypeId.HasValue || t.TargetStreakTypeId == streakTypeId)
                    )
                    || (
                        t.SourceKind == AchievementSourceKind.CumulativeAttendance
                        && (!t.TargetStreakTypeId.HasValue || t.TargetStreakTypeId == streakTypeId)
                    )
                )
            )
            .ToListAsync(cancellationToken);

        foreach (var type in candidates)
        {
            var count =
                type.SourceKind == AchievementSourceKind.StreakCount
                    ? currentOrLongestStreak
                    : totalEngagements;
            _ = await ApplySingleAsync(type, personId, count, asOfDate, cancellationToken);
        }
    }

    public async Task EvaluateMilestoneAndJourneyAchievementsAsync(
        Guid personId,
        Guid journeyProgramId,
        Guid milestoneTypeId,
        int completedMilestoneCount,
        int completedJourneyCount,
        DateOnly asOfDate,
        CancellationToken cancellationToken
    )
    {
        var candidates = await dbContext
            .AchievementTypes.AsNoTracking()
            .Where(t =>
                t.IsActive
                && (
                    t.SourceKind == AchievementSourceKind.MilestoneCompletions
                    || t.SourceKind == AchievementSourceKind.JourneyCompletions
                )
            )
            .ToListAsync(cancellationToken);

        foreach (var type in candidates)
        {
            if (
                type.SourceKind == AchievementSourceKind.MilestoneCompletions
                && (!type.TargetMilestoneTypeId.HasValue || type.TargetMilestoneTypeId == milestoneTypeId)
                && (!type.TargetJourneyProgramId.HasValue || type.TargetJourneyProgramId == journeyProgramId)
            )
            {
                _ = await ApplySingleAsync(
                    type,
                    personId,
                    completedMilestoneCount,
                    asOfDate,
                    cancellationToken
                );
            }
            else if (
                type.SourceKind == AchievementSourceKind.JourneyCompletions
                && (!type.TargetJourneyProgramId.HasValue || type.TargetJourneyProgramId == journeyProgramId)
            )
            {
                _ = await ApplySingleAsync(
                    type,
                    personId,
                    completedJourneyCount,
                    asOfDate,
                    cancellationToken
                );
            }
        }
    }

    private void EnqueueEarnedEvents(
        AchievementAttempt attempt,
        AchievementType achievementType,
        DateOnly achievedDate
    )
    {
        var now = clock.UtcNow;
        dbContext.EnqueueIntegrationEvent(
            new AchievementEarnedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                attempt.Id.Value,
                achievementType.Id.Value,
                achievementType.Code,
                attempt.PersonId,
                achievedDate
            )
        );

        if (!string.IsNullOrWhiteSpace(achievementType.CelebrationSystemCommunicationKey))
        {
            dbContext.EnqueueIntegrationEvent(
                new SystemNotificationRequestedIntegrationEvent(
                    Guid.CreateVersion7(),
                    now,
                    achievementType.CelebrationSystemCommunicationKey,
                    RecipientPersonId: attempt.PersonId,
                    MergeFields: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["AchievementName"] = achievementType.Name,
                        ["AchievementCode"] = achievementType.Code,
                    },
                    TriggerModule: "Engagement.Achievements"
                )
            );
        }
    }
}
