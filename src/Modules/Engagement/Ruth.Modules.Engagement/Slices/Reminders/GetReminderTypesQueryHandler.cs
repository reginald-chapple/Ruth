using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class GetReminderTypesQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetReminderTypesQuery, IReadOnlyList<ReminderTypeResponse>>
{
    public async Task<Result<IReadOnlyList<ReminderTypeResponse>>> HandleAsync(
        GetReminderTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.ReminderTypes.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return items.Select(ReminderProjectionMapper.ToResponse).ToArray();
    }
}
