using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Engagement.Contracts.Events;

/// <summary>Outbox integration event published when a ConstituentContact Touchpoint is completed.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when completed.</param>
/// <param name="TouchpointId">Completed touchpoint identifier.</param>
/// <param name="ConstituentContactId">Portfolio constituent contact identifier.</param>
/// <param name="OwnerPersonId">Portfolio owner / steward / caseworker person identifier.</param>
/// <param name="ConstituentPersonId">Contacted constituent person identifier.</param>
/// <param name="UpdatedHealthScore">Updated relationship health score (0-100).</param>
public sealed record TouchpointCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid TouchpointId,
    Guid ConstituentContactId,
    Guid OwnerPersonId,
    Guid ConstituentPersonId,
    int UpdatedHealthScore
) : IIntegrationEvent;
