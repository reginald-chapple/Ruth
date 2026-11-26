using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Commerce.Contracts.Events;

/// <summary>Outbox integration event emitted when a constituent's punch pass credit balance changes.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when the balance changed.</param>
/// <param name="IssuedPunchPassId">Changed punch pass identifier.</param>
/// <param name="PassCode">Unique punch pass code.</param>
/// <param name="OwnerPersonId">Constituent CRM person identifier.</param>
/// <param name="DeltaCredits">Signed credit change applied.</param>
/// <param name="PassRemainingCredits">Remaining credits on this specific pass.</param>
/// <param name="TotalActivePersonCredits">Total active non-expired credits across all passes for this person.</param>
public sealed record PunchPassBalanceChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid IssuedPunchPassId,
    string PassCode,
    Guid OwnerPersonId,
    int DeltaCredits,
    int PassRemainingCredits,
    int TotalActivePersonCredits
) : IIntegrationEvent;
