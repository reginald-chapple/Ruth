using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class GetEventCalendarsQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetEventCalendarsQuery, IReadOnlyList<EventCalendarResponse>>
{
    public async Task<Result<IReadOnlyList<EventCalendarResponse>>> HandleAsync(
        GetEventCalendarsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.EventCalendars.AsNoTracking().AsQueryable();
        if (query.PublicOnly)
        {
            q = q.Where(static c => c.IsPublic);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static c => c.IsActive);
        }

        var items = await q.OrderBy(static c => c.Name).ToListAsync(cancellationToken);
        return items.Select(EventCalendarMapper.MapCalendar).ToArray();
    }
}
