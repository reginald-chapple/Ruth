using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class CreateEventCalendarCommandHandler(EventsDbContext dbContext)
    : ICommandHandler<CreateEventCalendarCommand, EventCalendarResponse>
{
    public async Task<Result<EventCalendarResponse>> HandleAsync(
        CreateEventCalendarCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = EventCalendar.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<EventCalendarResponse>(created.Errors);
        }

        var slugExists = await dbContext.EventCalendars.AnyAsync(
            c => c.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugExists)
        {
            return EventsErrors.CalendarSlugConflict;
        }

        _ = dbContext.EventCalendars.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EventCalendarMapper.MapCalendar(created.Value);
    }
}
