using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Events.Contracts.Events;

/// <summary>Outbox integration event emitted when a Registration is submitted.</summary>
public sealed record EventRegistrationCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid RegistrationId,
    Guid InstanceId,
    string ConfirmationCode,
    Guid? RegistrarPersonId,
    string RegistrarEmail,
    int ConfirmedRegistrantCount,
    int WaitlistedRegistrantCount,
    decimal TotalAmount,
    decimal PaidAmount,
    Guid? LinkedInvoiceId
) : IIntegrationEvent;
