using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Communication.Contracts.Events;

/// <summary>
/// Outbox integration event enabling Finance, Workflow, Check-In, and custom modules to request
/// asynchronous transactional delivery of a SystemCommunication template.
/// </summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when requested.</param>
/// <param name="SystemCommunicationKey">Slug key of the target SystemCommunication (e.g., finance.payment-or-gift-receipt, workflow.task-assigned).</param>
/// <param name="RecipientEmail">Optional recipient email address.</param>
/// <param name="RecipientName">Optional recipient display name.</param>
/// <param name="RecipientPhoneNumber">Optional recipient E.164 SMS phone number.</param>
/// <param name="RecipientPersonId">Optional CRM person identifier for push device lookup.</param>
/// <param name="MergeFields">Optional declarative merge key/value tokens.</param>
/// <param name="TriggerModule">Originating module or workflow name.</param>
public sealed record SystemNotificationRequestedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    string SystemCommunicationKey,
    string? RecipientEmail = null,
    string? RecipientName = null,
    string? RecipientPhoneNumber = null,
    Guid? RecipientPersonId = null,
    IReadOnlyDictionary<string, string?>? MergeFields = null,
    string? TriggerModule = null
) : IIntegrationEvent;
