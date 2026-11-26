using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.SystemCommunications;

/// <summary>Immutable delivery audit record for a dispatched transactional system notification.</summary>
public sealed class SystemNotificationLog : Entity<SystemNotificationLogId>
{
    private SystemNotificationLog() { }

    private SystemNotificationLog(
        SystemNotificationLogId id,
        Guid systemCommunicationId,
        string systemCommunicationKey,
        CommunicationChannel channel,
        string transportKey,
        string recipientAddress,
        Guid? recipientPersonId,
        string? subjectOrTitle,
        string providerMessageId,
        string? triggerSource,
        DateTimeOffset sentOnUtc
    )
        : base(id)
    {
        SystemCommunicationId = systemCommunicationId;
        SystemCommunicationKey = systemCommunicationKey;
        Channel = channel;
        TransportKey = transportKey;
        RecipientAddress = recipientAddress;
        RecipientPersonId = recipientPersonId;
        SubjectOrTitle = subjectOrTitle;
        ProviderMessageId = providerMessageId;
        TriggerSource = triggerSource;
        SentOnUtc = sentOnUtc.ToUniversalTime();
    }

    public Guid SystemCommunicationId { get; private set; }
    public string SystemCommunicationKey { get; private set; } = string.Empty;
    public CommunicationChannel Channel { get; private set; }
    public string TransportKey { get; private set; } = string.Empty;
    public string RecipientAddress { get; private set; } = string.Empty;
    public Guid? RecipientPersonId { get; private set; }
    public string? SubjectOrTitle { get; private set; }
    public string ProviderMessageId { get; private set; } = string.Empty;
    public string? TriggerSource { get; private set; }
    public DateTimeOffset SentOnUtc { get; private set; }

    public static SystemNotificationLog Create(
        Guid systemCommunicationId,
        string systemCommunicationKey,
        CommunicationChannel channel,
        string transportKey,
        string recipientAddress,
        Guid? recipientPersonId,
        string? subjectOrTitle,
        string providerMessageId,
        string? triggerSource,
        DateTimeOffset sentOnUtc
    )
    {
        return new SystemNotificationLog(
            SystemNotificationLogId.Create(),
            systemCommunicationId,
            systemCommunicationKey,
            channel,
            transportKey,
            recipientAddress,
            recipientPersonId,
            subjectOrTitle,
            providerMessageId,
            triggerSource,
            sentOnUtc
        );
    }

    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (RecipientPersonId == retiredPersonId)
        {
            RecipientPersonId = survivingPersonId;
        }
    }
}
