using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal sealed class ProcessDueRemindersCommandHandler(EngagementDbContext dbContext, IClock clock)
    : ICommandHandler<ProcessDueRemindersCommand, ReminderProcessRunResponse>
{
    public async Task<Result<ReminderProcessRunResponse>> HandleAsync(
        ProcessDueRemindersCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var due = await dbContext
            .Reminders.Where(r => !r.IsComplete && r.ReminderDate <= today)
            .ToListAsync(cancellationToken);
        if (due.Count == 0)
        {
            return new ReminderProcessRunResponse(0);
        }

        var types = await dbContext
            .ReminderTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id.Value, cancellationToken);

        var notified = 0;
        foreach (var reminder in due)
        {
            if (
                types.TryGetValue(reminder.ReminderTypeId, out var type)
                && reminder.RecordNotificationAndAdvance(type, today, now)
            )
            {
                notified++;
                DispatchReminderNotification(reminder, type, now);
            }
        }

        if (notified > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ReminderProcessRunResponse(notified);
    }

    private void DispatchReminderNotification(
        Reminder reminder,
        ReminderType type,
        DateTimeOffset now
    )
    {
        _ = dbContext.NotificationMessages.Add(
            NotificationMessage.Create(
                reminder.AssigneePersonId,
                $"{type.Name}: {reminder.EntitySummary}",
                reminder.Note,
                NotificationCategory.Reminder,
                "/engagement/care",
                type.HighlightColorHex
            )
        );

        if (
            type.NotificationFlavor == ReminderNotificationFlavor.InAppAndSystemNotification
            && !string.IsNullOrWhiteSpace(type.SystemCommunicationKey)
        )
        {
            dbContext.EnqueueIntegrationEvent(
                new SystemNotificationRequestedIntegrationEvent(
                    Guid.CreateVersion7(),
                    now,
                    type.SystemCommunicationKey,
                    RecipientPersonId: reminder.AssigneePersonId,
                    MergeFields: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ReminderType"] = type.Name,
                        ["EntitySummary"] = reminder.EntitySummary,
                        ["Note"] = reminder.Note,
                    },
                    TriggerModule: "Engagement.Reminders"
                )
            );
        }
    }
}
