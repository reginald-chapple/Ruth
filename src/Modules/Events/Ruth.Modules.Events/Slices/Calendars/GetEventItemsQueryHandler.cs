using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class GetEventItemsQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetEventItemsQuery, IReadOnlyList<EventItemResponse>>
{
    public async Task<Result<IReadOnlyList<EventItemResponse>>> HandleAsync(
        GetEventItemsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext
            .EventItems.AsNoTracking()
            .Include(static i => i.CalendarItems)
            .Include(static i => i.Audiences)
            .Include(static i => i.Occurrences)
            .AsQueryable();

        if (query.CalendarId is { } calGuid && calGuid != Guid.Empty)
        {
            var calId = EventCalendarId.From(calGuid);
            q = q.Where(i => i.CalendarItems.Any(c => c.CalendarId == calId));
        }

        if (query.BranchId is { } branchId && branchId != Guid.Empty)
        {
            q = q.Where(i => i.Occurrences.Any(o => o.BranchId == branchId));
        }

        if (query.AudienceDefinedValueId is { } audId && audId != Guid.Empty)
        {
            q = q.Where(i => i.Audiences.Any(a => a.AudienceDefinedValueId == audId));
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static i => i.IsActive);
        }

        if (query.PublicApprovedOnly)
        {
            q = q.Where(static i =>
                i.IsActive && i.IsPublic && i.ApprovalStatus == EventItemApprovalStatus.Approved
            );
        }

        var items = await q.OrderBy(static i => i.Name).ToListAsync(cancellationToken);
        return items.Select(EventCalendarMapper.MapEventItem).ToArray();
    }
}
