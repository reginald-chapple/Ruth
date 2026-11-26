using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Domain.Reminders;

namespace Ruth.Modules.Engagement.Slices.Reminders;

internal static class ReminderProjectionMapper
{
    public static ReminderTypeResponse ToResponse(ReminderType entity)
    {
        return new ReminderTypeResponse(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.EntityTypeKeyFilter,
            entity.HighlightColorHex,
            entity.ShouldAutoCompleteWhenNotified,
            entity.NotificationFlavor,
            entity.SystemCommunicationKey,
            entity.IsActive
        );
    }

    public static ReminderResponse ToResponse(Reminder entity, ReminderType type)
    {
        return new ReminderResponse(
            entity.Id.Value,
            entity.ReminderTypeId,
            type.Name,
            type.HighlightColorHex,
            entity.AssigneePersonId,
            entity.TargetEntityTypeKey,
            entity.TargetEntityId,
            entity.EntitySummary,
            entity.Note,
            entity.ReminderDate,
            entity.RenewPeriodDays,
            entity.RenewMaxCount,
            entity.RenewCurrentCount,
            entity.IsComplete,
            entity.CompletedAtUtc,
            entity.LastNotifiedAtUtc
        );
    }

    public static NotificationMessageResponse ToResponse(NotificationMessage entity)
    {
        return new NotificationMessageResponse(
            entity.Id.Value,
            entity.RecipientPersonId,
            entity.Title,
            entity.Message,
            entity.Category,
            entity.ActionUrl,
            entity.ColorHex,
            entity.IsRead,
            entity.ReadAtUtc,
            entity.CreatedOnUtc
        );
    }
}
