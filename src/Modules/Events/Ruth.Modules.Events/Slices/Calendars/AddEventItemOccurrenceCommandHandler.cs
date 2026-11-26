using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Calendars;

internal sealed class AddEventItemOccurrenceCommandHandler(EventsDbContext dbContext)
    : ICommandHandler<AddEventItemOccurrenceCommand, EventItemOccurrenceResponse>
{
    public async Task<Result<EventItemOccurrenceResponse>> HandleAsync(
        AddEventItemOccurrenceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = EventItemId.From(command.EventItemId);
        var item = await dbContext
            .EventItems.Include(static i => i.Occurrences)
            .FirstOrDefaultAsync(i => i.Id == targetId, cancellationToken);
        if (item is null)
        {
            return EventsErrors.EventItemNotFound;
        }

        var added = item.AddOccurrence(command.Spec);
        if (added.IsFailure)
        {
            return Result.Failure<EventItemOccurrenceResponse>(added.Errors);
        }

        _ = dbContext.EventItemOccurrences.Add(added.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EventCalendarMapper.MapOccurrence(added.Value);
    }
}
