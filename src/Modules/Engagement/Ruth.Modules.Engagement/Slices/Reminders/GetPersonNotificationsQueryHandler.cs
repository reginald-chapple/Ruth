using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class GetPersonNotificationsQueryHandler(EngagementDbContext dbContext)
    : IQueryHandler<GetPersonNotificationsQuery, IReadOnlyList<NotificationMessageResponse>>
{
    public async Task<Result<IReadOnlyList<NotificationMessageResponse>>> HandleAsync(
        GetPersonNotificationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = dbContext.NotificationMessages.AsNoTracking().AsQueryable();
        if (query.RecipientPersonId is { } pid && pid != Guid.Empty)
        {
            q = q.Where(n => n.RecipientPersonId == pid);
        }

        if (query.UnreadOnly)
        {
            q = q.Where(static n => !n.IsRead);
        }

        var items = await q.OrderBy(static n => n.IsRead)
            .ThenByDescending(static n => n.CreatedOnUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        return items.Select(ReminderProjectionMapper.ToResponse).ToArray();
    }
}
