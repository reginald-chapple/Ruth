using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class EnrollInStreakCommandHandler(EngagementDbContext dbContext, IClock clock)
    : ICommandHandler<EnrollInStreakCommand, StreakResponse>
{
    public async Task<Result<StreakResponse>> HandleAsync(
        EnrollInStreakCommand command,
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

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var existing = await dbContext.Streaks.FirstOrDefaultAsync(
            s => s.StreakTypeId == command.StreakTypeId && s.PersonId == command.PersonId,
            cancellationToken
        );
        if (existing is not null)
        {
            return StreakProjectionMapper.ToResponse(existing, streakType, today);
        }

        var created = Streak.Enroll(
            streakType,
            command.PersonId,
            command.BranchId,
            command.EnrollmentDate ?? today
        );
        if (created.IsFailure)
        {
            return Result.Failure<StreakResponse>(created.Errors);
        }

        _ = dbContext.Streaks.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StreakProjectionMapper.ToResponse(created.Value, streakType, today);
    }
}
