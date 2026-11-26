using Ruth.SharedKernel.Events;

namespace Ruth.Modules.CheckIn.Contracts.Events;

/// <summary>Outbox integration event published when a participant is checked out.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when check-out occurred.</param>
/// <param name="SessionId">Check-in session identifier.</param>
/// <param name="EntryId">Check-in attendance entry identifier.</param>
/// <param name="PersonId">Participant person identifier.</param>
/// <param name="LocationId">Room/location identifier.</param>
/// <param name="ServiceDate">Local service date.</param>
/// <param name="CheckedOutByPersonId">Optional guardian/staff person identifier.</param>
public sealed record ParticipantCheckedOutIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid SessionId,
    Guid EntryId,
    Guid PersonId,
    Guid LocationId,
    DateOnly ServiceDate,
    Guid? CheckedOutByPersonId
) : IIntegrationEvent;
