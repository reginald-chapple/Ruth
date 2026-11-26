using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class UpdateEventCalendarCommandHandler(EventsDbContext dbContext)
    : ICommandHandler<UpdateEventCalendarCommand, EventCalendarResponse>
{
    public async Task<Result<EventCalendarResponse>> HandleAsync(
        UpdateEventCalendarCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = EventCalendarId.From(command.CalendarId);
        var calendar = await dbContext.EventCalendars.FirstOrDefaultAsync(
            c => c.Id == targetId,
            cancellationToken
        );
        if (calendar is null)
        {
            return EventsErrors.CalendarNotFound;
        }

        var updated = calendar.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<EventCalendarResponse>(updated.Errors);
        }

        var slugConflict = await dbContext.EventCalendars.AnyAsync(
            c => c.Id != targetId && c.Slug == calendar.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return EventsErrors.CalendarSlugConflict;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EventCalendarMapper.MapCalendar(calendar);
    }
}
