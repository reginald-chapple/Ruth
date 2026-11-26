using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class CreateEventItemCommandHandler(
    EventsDbContext dbContext,
    TimeProvider timeProvider
) : ICommandHandler<CreateEventItemCommand, EventItemResponse>
{
    public async Task<Result<EventItemResponse>> HandleAsync(
        CreateEventItemCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = EventItem.Create(command.Spec, timeProvider.GetUtcNow());
        if (created.IsFailure)
        {
            return Result.Failure<EventItemResponse>(created.Errors);
        }

        var calendarIds = created.Value.CalendarItems.Select(static c => c.CalendarId).ToArray();
        var existingCalendarsCount = await dbContext.EventCalendars.CountAsync(
            c => calendarIds.Contains(c.Id),
            cancellationToken
        );
        if (existingCalendarsCount != calendarIds.Length)
        {
            return EventsErrors.CalendarNotFound;
        }

        var slugConflict = await dbContext.EventItems.AnyAsync(
            i => i.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return EventsErrors.EventItemSlugConflict;
        }

        _ = dbContext.EventItems.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EventCalendarMapper.MapEventItem(created.Value);
    }
}
