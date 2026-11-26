using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class GetAchievementAttemptsQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetAchievementAttemptsQuery, IReadOnlyList<AchievementAttemptResponse>>
{
    public async Task<Result<IReadOnlyList<AchievementAttemptResponse>>> HandleAsync(
        GetAchievementAttemptsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.AchievementAttempts.AsNoTracking().AsQueryable();
        if (query.AchievementTypeId is { } aid && aid != Guid.Empty)
        {
            q = q.Where(a => a.AchievementTypeId == aid);
        }

        if (query.PersonId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(a => a.PersonId == pid);
        }

        if (query.SuccessfulOnly)
        {
            q = q.Where(static a => a.IsSuccessful);
        }

        var attempts = await q
            .OrderByDescending(static a => a.AttemptStartDate)
            .ToListAsync(cancellationToken);
        var types = await dbContext
            .AchievementTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id.Value, cancellationToken);

        return attempts
            .Where(a => types.ContainsKey(a.AchievementTypeId))
            .Select(a => AchievementProjectionMapper.ToResponse(a, types[a.AchievementTypeId]))
            .ToArray();
    }
}
