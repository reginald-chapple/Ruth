using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class CompleteReminderCommandHandler(EngagementDbContext dbContext, IClock clock)
    : ICommandHandler<CompleteReminderCommand, ReminderResponse>
{
    public async Task<Result<ReminderResponse>> HandleAsync(
        CompleteReminderCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var reminder = await dbContext.Reminders.FirstOrDefaultAsync(
            r => r.Id == ReminderId.From(command.ReminderId),
            cancellationToken
        );
        if (reminder is null)
        {
            return EngagementErrors.ReminderNotFound;
        }

        var type = await dbContext
            .ReminderTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.Id == ReminderTypeId.From(reminder.ReminderTypeId),
                cancellationToken
            );
        if (type is null)
        {
            return EngagementErrors.ReminderTypeNotFound;
        }

        reminder.MarkComplete(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ReminderProjectionMapper.ToResponse(reminder, type);
    }
}
