using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Audit log entry recording a dispatched transactional system notification.</summary>
/// <param name="Id">Unique log entry identifier.</param>
/// <param name="SystemCommunicationId">System communication template identifier.</param>
/// <param name="SystemCommunicationKey">System communication template key.</param>
/// <param name="Channel">Dispatched channel.</param>
/// <param name="TransportKey">Transport provider key.</param>
/// <param name="RecipientAddress">Destination email, phone number, or push token.</param>
/// <param name="RecipientPersonId">Optional CRM person identifier.</param>
/// <param name="SubjectOrTitle">Rendered subject or push title.</param>
/// <param name="ProviderMessageId">Provider correlation message identifier.</param>
/// <param name="TriggerSource">Module or event source that triggered dispatch.</param>
/// <param name="SentOnUtc">UTC timestamp when dispatched.</param>
public sealed record SystemNotificationLogResponse(
    Guid Id,
    Guid SystemCommunicationId,
    string SystemCommunicationKey,
    CommunicationChannel Channel,
    string TransportKey,
    string RecipientAddress,
    Guid? RecipientPersonId,
    string? SubjectOrTitle,
    string ProviderMessageId,
    string? TriggerSource,
    DateTimeOffset SentOnUtc
);
