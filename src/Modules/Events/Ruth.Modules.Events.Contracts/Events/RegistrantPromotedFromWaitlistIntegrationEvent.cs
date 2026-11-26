using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Events.Contracts.Events;

/// <summary>Outbox integration event emitted when a waitlisted registrant is automatically promoted to a confirmed seat.</summary>
public sealed record RegistrantPromotedFromWaitlistIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid RegistrationId,
    Guid RegistrantId,
    Guid InstanceId,
    Guid? PersonId,
    string FirstName,
    string LastName,
    string? Email
) : IIntegrationEvent;
