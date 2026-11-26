using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class GetStreakTypesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetStreakTypesQuery, IReadOnlyList<StreakTypeResponse>>
{
    public async Task<Result<IReadOnlyList<StreakTypeResponse>>> HandleAsync(
        GetStreakTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext
            .StreakTypes.AsNoTracking()
            .Include(static s => s.Exclusions)
            .AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static s => s.IsActive);
        }

        var items = await q.OrderBy(static s => s.Name).ToListAsync(cancellationToken);
        return items.Select(StreakProjectionMapper.ToResponse).ToArray();
    }
}
