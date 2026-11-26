using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class UpdateEventItemCommandHandler(
    EventsDbContext dbContext,
    TimeProvider timeProvider
) : ICommandHandler<UpdateEventItemCommand, EventItemResponse>
{
    public async Task<Result<EventItemResponse>> HandleAsync(
        UpdateEventItemCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = EventItemId.From(command.EventItemId);
        var item = await dbContext
            .EventItems.Include(static i => i.CalendarItems)
            .Include(static i => i.Audiences)
            .Include(static i => i.Occurrences)
            .FirstOrDefaultAsync(i => i.Id == targetId, cancellationToken);
        if (item is null)
        {
            return EventsErrors.EventItemNotFound;
        }

        var existingCalendarRowIds = item.CalendarItems.Select(static c => c.Id).ToHashSet();
        var updated = item.Update(command.Spec, timeProvider.GetUtcNow());
        if (updated.IsFailure)
        {
            return Result.Failure<EventItemResponse>(updated.Errors);
        }

        var slugConflict = await dbContext.EventItems.AnyAsync(
            i => i.Id != targetId && i.Slug == item.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return EventsErrors.EventItemSlugConflict;
        }

        foreach (
            var calRow in item.CalendarItems.Where(c => !existingCalendarRowIds.Contains(c.Id))
        )
        {
            _ = dbContext.EventCalendarItems.Add(calRow);
        }

        foreach (var aud in item.Audiences)
        {
            _ = dbContext.EventItemAudiences.Add(aud);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EventCalendarMapper.MapEventItem(item);
    }
}
