using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain.SystemCommunications;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal static class SystemCommunicationMapper
{
    public static SystemCommunicationResponse ToResponse(SystemCommunication entity)
    {
        return new SystemCommunicationResponse(
            entity.Id.Value,
            entity.SystemGuid,
            entity.Title,
            entity.Key,
            entity.Category,
            entity.FromName,
            entity.FromEmail,
            entity.SubjectTemplate,
            entity.HtmlBodyTemplate,
            entity.RazorLayoutKey,
            entity.SmsFromSystemPhoneNumberId,
            entity.SmsMessageTemplate,
            entity.PushTitleTemplate,
            entity.PushMessageTemplate,
            entity.PushOpenUrlTemplate,
            entity.IsSystem,
            entity.IsActive
        );
    }

    public static SystemNotificationLogResponse ToLogResponse(SystemNotificationLog log)
    {
        return new SystemNotificationLogResponse(
            log.Id.Value,
            log.SystemCommunicationId,
            log.SystemCommunicationKey,
            log.Channel,
            log.TransportKey,
            log.RecipientAddress,
            log.RecipientPersonId,
            log.SubjectOrTitle,
            log.ProviderMessageId,
            log.TriggerSource,
            log.SentOnUtc
        );
    }
}
