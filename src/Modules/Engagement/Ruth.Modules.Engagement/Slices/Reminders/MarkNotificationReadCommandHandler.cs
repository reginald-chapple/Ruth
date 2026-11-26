using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class MarkNotificationReadCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<MarkNotificationReadCommand, NotificationMessageResponse>
{
    public async Task<Result<NotificationMessageResponse>> HandleAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await dbContext.NotificationMessages.FirstOrDefaultAsync(
            n => n.Id == NotificationMessageId.From(command.NotificationId),
            cancellationToken
        );
        if (entity is null)
        {
            return EngagementErrors.NotificationNotFound;
        }

        entity.MarkRead(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ReminderProjectionMapper.ToResponse(entity);
    }
}
