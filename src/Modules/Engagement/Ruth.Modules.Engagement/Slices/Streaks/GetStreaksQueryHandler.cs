using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class GetStreaksQueryHandler(EngagementDbContext dbContext, IClock clock)
    : IQueryHandler<GetStreaksQuery, IReadOnlyList<StreakResponse>>
{
    public async Task<Result<IReadOnlyList<StreakResponse>>> HandleAsync(
        GetStreaksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.Streaks.AsNoTracking().AsQueryable();
        if (query.StreakTypeId is { } stId && stId != Guid.Empty)
        {
            q = q.Where(s => s.StreakTypeId == stId);
        }

        if (query.PersonId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(s => s.PersonId == pid);
        }

        var streaks = await q.OrderByDescending(static s => s.CurrentStreakCount)
            .ThenByDescending(static s => s.EngagementCount)
            .ToListAsync(cancellationToken);

        var types = await dbContext
            .StreakTypes.AsNoTracking()
            .Include(static s => s.Exclusions)
            .ToDictionaryAsync(static s => s.Id.Value, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        return streaks
            .Where(s => types.ContainsKey(s.StreakTypeId))
            .Select(s => StreakProjectionMapper.ToResponse(s, types[s.StreakTypeId], today))
            .ToArray();
    }
}
