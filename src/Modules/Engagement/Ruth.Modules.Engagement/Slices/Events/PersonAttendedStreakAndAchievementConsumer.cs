using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Achievements;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Events;

/// <summary>Updates matching Streak bitmasks and AchievementAttempts when Groups attendance is recorded.</summary>
internal sealed class PersonAttendedStreakAndAchievementConsumer(
    EngagementDbContext dbContext,
    AchievementProgressionCollaborator achievements,
    IClock clock
) : IIntegrationEventConsumer<PersonAttendedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonAttendedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (integrationEvent.PersonId == Guid.Empty)
        {
            return;
        }

        var activeTypes = await dbContext
            .StreakTypes.AsNoTracking()
            .Include(static s => s.Exclusions)
            .Where(static s => s.IsActive)
            .ToListAsync(cancellationToken);

        var matchingTypes = activeTypes
            .Where(t => t.MatchesAttendance(integrationEvent.GroupId, integrationEvent.GroupTypeId))
            .ToArray();
        if (matchingTypes.Length == 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var changed = false;

        foreach (var streakType in matchingTypes)
        {
            var streak = await dbContext.Streaks.FirstOrDefaultAsync(
                s =>
                    s.StreakTypeId == streakType.Id.Value
                    && s.PersonId == integrationEvent.PersonId,
                cancellationToken
            );
            if (streak is null)
            {
                if (streakType.RequiresExplicitEnrollment)
                {
                    continue;
                }

                var enrolled = Streak.Enroll(
                    streakType,
                    integrationEvent.PersonId,
                    integrationEvent.BranchId,
                    integrationEvent.OccurrenceDate
                );
                if (enrolled.IsFailure)
                {
                    continue;
                }

                streak = enrolled.Value;
                _ = dbContext.Streaks.Add(streak);
            }

            if (
                streak
                    .RecordEngagement(
                        streakType,
                        integrationEvent.OccurrenceDate,
                        today,
                        integrationEvent.BranchId
                    )
                    .IsSuccess
            )
            {
                changed = true;
                if (streakType.EnableAchievements)
                {
                    await achievements.EvaluateStreakAchievementsAsync(
                        streakType.Id.Value,
                        integrationEvent.PersonId,
                        Math.Max(streak.CurrentStreakCount, streak.LongestStreakCount),
                        streak.EngagementCount,
                        integrationEvent.OccurrenceDate,
                        cancellationToken
                    );
                }
            }
        }

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
