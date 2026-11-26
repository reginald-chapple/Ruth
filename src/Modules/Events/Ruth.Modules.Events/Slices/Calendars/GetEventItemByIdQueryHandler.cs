using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class GetEventItemByIdQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetEventItemByIdQuery, EventItemResponse>
{
    public async Task<Result<EventItemResponse>> HandleAsync(
        GetEventItemByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var targetId = EventItemId.From(query.EventItemId);
        var item = await dbContext
            .EventItems.AsNoTracking()
            .Include(static i => i.CalendarItems)
            .Include(static i => i.Audiences)
            .Include(static i => i.Occurrences)
            .FirstOrDefaultAsync(i => i.Id == targetId, cancellationToken);

        return item is null
            ? EventsErrors.EventItemNotFound
            : EventCalendarMapper.MapEventItem(item);
    }
}
