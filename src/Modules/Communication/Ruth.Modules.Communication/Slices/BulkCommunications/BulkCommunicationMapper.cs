using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal static class BulkCommunicationMapper
{
    public static CommunicationTemplateResponse ToResponse(CommunicationTemplate entity)
    {
        return new CommunicationTemplateResponse(
            entity.Id.Value,
            entity.Name,
            entity.Key,
            entity.Description,
            entity.Channel,
            entity.TopicKey,
            entity.SubjectTemplate,
            entity.PreheaderText,
            entity.FromName,
            entity.FromEmail,
            entity.ReplyToEmail,
            entity.SenderSystemPhoneNumberId,
            entity.RazorLayoutKey,
            entity.SmsBodyTemplate,
            entity.PushTitleTemplate,
            entity.PushBodyTemplate,
            [.. entity.Sections.Select(static s => s.ToDto())],
            entity.IsSystem,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static CommunicationRecipientResponse ToResponse(CommunicationRecipient entity)
    {
        return new CommunicationRecipientResponse(
            entity.Id.Value,
            entity.CommunicationId.Value,
            entity.PersonId,
            entity.DestinationAddress,
            entity.DisplayName,
            entity.Status,
            entity.TransportKey,
            entity.ProviderMessageId,
            entity.BounceKind,
            entity.OpenedCount,
            entity.FirstOpenedAtUtc,
            entity.ClickedCount,
            entity.FirstClickedAtUtc,
            entity.LastClickedUrl,
            entity.SentAtUtc,
            entity.StatusNote
        );
    }

    public static CommunicationDetailResponse ToDetailResponse(
        Domain.BulkCommunications.Communication entity,
        IReadOnlyList<CommunicationRecipient> recipients
    )
    {
        return new CommunicationDetailResponse(
            entity.Id.Value,
            entity.Title,
            entity.Channel,
            entity.Status,
            entity.TopicKey,
            entity.IsBulk,
            entity.TemplateId,
            entity.Subject,
            entity.PreheaderText,
            entity.FromName,
            entity.FromEmail,
            entity.ReplyToEmail,
            entity.SenderSystemPhoneNumberId,
            entity.RazorLayoutKey,
            entity.HtmlBodyOverride,
            entity.SmsBody,
            entity.PushTitle,
            entity.PushBody,
            entity.PushOpenUrl,
            [.. entity.Sections.Select(static s => s.ToDto())],
            entity.CreatedByPersonId,
            entity.ReviewerPersonId,
            entity.ApprovalNote,
            entity.ScheduledSendAtUtc,
            entity.SentAtUtc,
            [.. recipients.Select(ToResponse)],
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static CommunicationSummaryResponse ToSummaryResponse(
        Domain.BulkCommunications.Communication entity,
        IReadOnlyList<CommunicationRecipient> recipients
    )
    {
        return new CommunicationSummaryResponse(
            entity.Id.Value,
            entity.Title,
            entity.Channel,
            entity.Status,
            entity.TopicKey,
            entity.IsBulk,
            entity.Subject,
            recipients.Count,
            recipients.Count(static r => r.Status == RecipientDeliveryStatus.Delivered),
            recipients.Count(static r => r.Status == RecipientDeliveryStatus.Failed),
            recipients.Count(static r => r.Status == RecipientDeliveryStatus.SkippedUnsubscribed),
            recipients.Count(static r => r.Status == RecipientDeliveryStatus.Bounced),
            recipients.Count(static r => r.OpenedCount > 0),
            recipients.Count(static r => r.ClickedCount > 0),
            entity.ScheduledSendAtUtc,
            entity.SentAtUtc,
            entity.CreatedOnUtc
        );
    }
}
