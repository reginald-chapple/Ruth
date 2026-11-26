using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class AddStreakExclusionCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<AddStreakExclusionCommand, StreakTypeResponse>
{
    public async Task<Result<StreakTypeResponse>> HandleAsync(
        AddStreakExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = StreakTypeId.From(command.StreakTypeId);
        var streakType = await dbContext
            .StreakTypes.Include(static s => s.Exclusions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (streakType is null)
        {
            return EngagementErrors.StreakTypeNotFound;
        }

        var addRes = streakType.AddExclusion(command.ExclusionDate, command.BranchId);
        if (addRes.IsFailure)
        {
            return Result.Failure<StreakTypeResponse>(addRes.Errors);
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var streaks = await dbContext
            .Streaks.Where(s => s.StreakTypeId == command.StreakTypeId)
            .ToListAsync(cancellationToken);
        foreach (var streak in streaks)
        {
            streak.RebuildMetrics(streakType, today);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StreakProjectionMapper.ToResponse(streakType);
    }
}
