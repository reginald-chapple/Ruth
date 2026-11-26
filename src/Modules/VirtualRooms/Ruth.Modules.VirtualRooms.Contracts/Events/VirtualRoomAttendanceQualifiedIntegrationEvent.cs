using Ruth.SharedKernel.Events;

namespace Ruth.Modules.VirtualRooms.Contracts.Events;

/// <summary>Outbox integration event emitted when a VirtualRoom participant crosses MinimumAttendanceDwellSeconds.</summary>
/// <param name="EventId">Unique integration event identifier.</param>
/// <param name="OccurredOnUtc">UTC timestamp when qualified.</param>
/// <param name="VirtualRoomId">Owning VirtualRoom identifier.</param>
/// <param name="SessionId">Owning VirtualRoomSession identifier.</param>
/// <param name="PersonId">Qualified CRM Person identifier.</param>
/// <param name="LinkedGroupId">Optional linked Group identifier.</param>
/// <param name="CumulativeDwellSeconds">Accumulated dwell seconds.</param>
public sealed record VirtualRoomAttendanceQualifiedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid VirtualRoomId,
    Guid SessionId,
    Guid PersonId,
    Guid? LinkedGroupId,
    int CumulativeDwellSeconds
) : IIntegrationEvent;
