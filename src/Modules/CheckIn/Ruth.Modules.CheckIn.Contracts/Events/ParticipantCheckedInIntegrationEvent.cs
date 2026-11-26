using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.CheckIn.Contracts.Events;

/// <summary>Outbox integration event published when a participant completes check-in.</summary>
/// <param name="EventId">Unique event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when check-in completed.</param>
/// <param name="SessionId">Check-in session identifier.</param>
/// <param name="EntryId">Check-in attendance entry identifier.</param>
/// <param name="ConfigurationId">Check-in configuration identifier.</param>
/// <param name="AreaId">Check-in area identifier.</param>
/// <param name="LinkedGroupId">Optional linked Group identifier.</param>
/// <param name="PersonId">Participant person identifier.</param>
/// <param name="LocationId">Assigned room/location identifier.</param>
/// <param name="ScheduleId">Schedule identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="ServiceDate">Local service date.</param>
/// <param name="Channel">Check-in channel.</param>
/// <param name="SecurityPickupCode">Optional issued daily pickup security code.</param>
public sealed record ParticipantCheckedInIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid SessionId,
    Guid EntryId,
    Guid ConfigurationId,
    Guid AreaId,
    Guid? LinkedGroupId,
    Guid PersonId,
    Guid LocationId,
    Guid ScheduleId,
    Guid? BranchId,
    DateOnly ServiceDate,
    CheckInSessionChannel Channel,
    string? SecurityPickupCode
) : IIntegrationEvent;
