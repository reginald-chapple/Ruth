using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when a Community Care / SupportRequest is submitted.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when submitted.</param>
/// <param name="SupportRequestId">Support request identifier.</param>
/// <param name="SupportCategoryId">Support category identifier.</param>
/// <param name="RequesterPersonId">Optional requester person identifier.</param>
/// <param name="BranchId">Optional branch identifier.</param>
/// <param name="IsUrgent">Indicates whether the support request is flagged urgent.</param>
public sealed record SupportRequestSubmittedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid SupportRequestId,
    Guid SupportCategoryId,
    Guid? RequesterPersonId,
    Guid? BranchId,
    bool IsUrgent
) : IIntegrationEvent;
