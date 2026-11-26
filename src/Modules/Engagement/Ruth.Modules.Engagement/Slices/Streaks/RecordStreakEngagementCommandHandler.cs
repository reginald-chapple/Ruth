using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Achievements;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class RecordStreakEngagementCommandHandler(
    EngagementDbContext dbContext,
    AchievementProgressionCollaborator achievements,
    IClock clock
) : ICommandHandler<RecordStreakEngagementCommand, StreakResponse>
{
    public async Task<Result<StreakResponse>> HandleAsync(
        RecordStreakEngagementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var streakType = await dbContext
            .StreakTypes.AsNoTracking()
            .Include(static s => s.Exclusions)
            .FirstOrDefaultAsync(
                s => s.Id == StreakTypeId.From(command.StreakTypeId),
                cancellationToken
            );
        if (streakType is null)
        {
            return EngagementErrors.StreakTypeNotFound;
        }

        var streakRes = await GetOrEnrollStreakAsync(command, streakType, cancellationToken);
        if (streakRes.IsFailure)
        {
            return Result.Failure<StreakResponse>(streakRes.Errors);
        }

        var streak = streakRes.Value;
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var rec = streak.RecordEngagement(
            streakType,
            command.EngagementDate,
            today,
            command.BranchId
        );
        if (rec.IsFailure)
        {
            return Result.Failure<StreakResponse>(rec.Errors);
        }

        if (streakType.EnableAchievements)
        {
            await achievements.EvaluateStreakAchievementsAsync(
                streakType.Id.Value,
                command.PersonId,
                Math.Max(streak.CurrentStreakCount, streak.LongestStreakCount),
                streak.EngagementCount,
                command.EngagementDate,
                cancellationToken
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StreakProjectionMapper.ToResponse(streak, streakType, today);
    }

    private async Task<Result<Streak>> GetOrEnrollStreakAsync(
        RecordStreakEngagementCommand command,
        StreakType streakType,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.Streaks.FirstOrDefaultAsync(
            s => s.StreakTypeId == command.StreakTypeId && s.PersonId == command.PersonId,
            cancellationToken
        );
        if (existing is not null)
        {
            return existing;
        }

        var enrolled = Streak.Enroll(
            streakType,
            command.PersonId,
            command.BranchId,
            command.EngagementDate
        );
        if (enrolled.IsSuccess)
        {
            _ = dbContext.Streaks.Add(enrolled.Value);
        }

        return enrolled;
    }
}
