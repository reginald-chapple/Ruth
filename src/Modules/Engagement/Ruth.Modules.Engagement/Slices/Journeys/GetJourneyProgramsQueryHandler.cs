using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class GetJourneyProgramsQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetJourneyProgramsQuery, IReadOnlyList<JourneyProgramResponse>>
{
    public async Task<Result<IReadOnlyList<JourneyProgramResponse>>> HandleAsync(
        GetJourneyProgramsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext
            .JourneyPrograms.AsNoTracking()
            .Include(static p => p.Statuses)
            .Include(static p => p.MilestoneTypes)
            .AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.IsActive);
        }

        var items = await q.OrderBy(static p => p.Name).ToListAsync(cancellationToken);
        return items.Select(JourneyProjectionMapper.ToResponse).ToArray();
    }
}
