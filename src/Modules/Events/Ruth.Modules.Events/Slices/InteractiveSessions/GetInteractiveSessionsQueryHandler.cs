using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class GetInteractiveSessionsQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetInteractiveSessionsQuery, IReadOnlyList<InteractiveSessionDetailResponse>>
{
    public async Task<Result<IReadOnlyList<InteractiveSessionDetailResponse>>> HandleAsync(
        GetInteractiveSessionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext
            .InteractiveSessions.AsNoTracking()
            .Include(static s => s.Actions)
            .Include(static s => s.Occurrences)
            .Include(static s => s.Responses)
            .AsQueryable();

        if (query.Status.HasValue)
        {
            q = q.Where(s => s.Status == query.Status.Value);
        }

        var items = await q.OrderByDescending(static s => s.CreatedOnUtc)
            .ToListAsync(cancellationToken);
        return items.Select(InteractiveSessionMapper.MapDetail).ToArray();
    }
}
