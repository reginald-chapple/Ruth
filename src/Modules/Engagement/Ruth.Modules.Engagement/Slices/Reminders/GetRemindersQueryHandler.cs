using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class GetRemindersQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetRemindersQuery, IReadOnlyList<ReminderResponse>>
{
    public async Task<Result<IReadOnlyList<ReminderResponse>>> HandleAsync(
        GetRemindersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.Reminders.AsNoTracking().AsQueryable();
        if (query.AssigneePersonId is { } aid && aid != Guid.Empty)
        {
            q = q.Where(r => r.AssigneePersonId == aid);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityTypeKey))
        {
            var key = query.EntityTypeKey.Trim();
            q = q.Where(r => r.TargetEntityTypeKey == key);
        }

        if (query.EntityId is { } eid && eid != Guid.Empty)
        {
            q = q.Where(r => r.TargetEntityId == eid);
        }

        if (!query.IncludeCompleted)
        {
            q = q.Where(static r => !r.IsComplete);
        }

        var items = await q.OrderBy(static r => r.ReminderDate).ToListAsync(cancellationToken);
        var types = await dbContext
            .ReminderTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id.Value, cancellationToken);

        return items
            .Where(r => types.ContainsKey(r.ReminderTypeId))
            .Select(r => ReminderProjectionMapper.ToResponse(r, types[r.ReminderTypeId]))
            .ToArray();
    }
}
