using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class GetAchievementTypesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetAchievementTypesQuery, IReadOnlyList<AchievementTypeResponse>>
{
    public async Task<Result<IReadOnlyList<AchievementTypeResponse>>> HandleAsync(
        GetAchievementTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.AchievementTypes.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static a => a.IsActive);
        }

        var items = await q.OrderBy(static a => a.Name).ToListAsync(cancellationToken);
        return items.Select(AchievementProjectionMapper.ToResponse).ToArray();
    }
}
