namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Read projection of an embedded VirtualRoom.</summary>
/// <param name="Id">VirtualRoom identifier.</param>
/// <param name="Name">Room title.</param>
/// <param name="Slug">Unique URL slug.</param>
/// <param name="LiveKitRoomName">Canonical LiveKit room name.</param>
/// <param name="ContextKind">Domain linkage kind.</param>
/// <param name="Description">Optional description.</param>
/// <param name="LinkedGroupId">Optional Group identifier.</param>
/// <param name="LinkedLocationId">Optional Location identifier.</param>
/// <param name="LinkedScheduleId">Optional Schedule identifier.</param>
/// <param name="LinkedBranchId">Optional Branch identifier.</param>
/// <param name="LinkedBookingId">Optional Booking identifier.</param>
/// <param name="LinkedSupportRequestId">Optional SupportRequest identifier.</param>
/// <param name="LinkedEventItemOccurrenceId">Optional EventItemOccurrence identifier.</param>
/// <param name="LinkedLmsClassId">Optional LMS Class identifier.</param>
/// <param name="ExternalReferenceKey">Optional correlation key.</param>
/// <param name="HostModeratorPersonId">Optional host moderator Person identifier.</param>
/// <param name="MaxParticipants">Maximum participants.</param>
/// <param name="EmptyTimeoutSeconds">Empty room timeout seconds.</param>
/// <param name="AutoRecordAttendance">Whether auto-attendance is enabled.</param>
/// <param name="MinimumAttendanceDwellSeconds">Required dwell seconds for attendance.</param>
/// <param name="AutoStartRecordingEgress">Whether recording starts automatically.</param>
/// <param name="IsWebinarBroadcast">Whether standard viewers join as Observers.</param>
/// <param name="IsActive">Whether active.</param>
/// <param name="CreatedAtUtc">Creation UTC timestamp.</param>
public sealed record VirtualRoomResponse(
    Guid Id,
    string Name,
    string Slug,
    string LiveKitRoomName,
    VirtualRoomContextKind ContextKind,
    string? Description,
    Guid? LinkedGroupId,
    Guid? LinkedLocationId,
    Guid? LinkedScheduleId,
    Guid? LinkedBranchId,
    Guid? LinkedBookingId,
    Guid? LinkedSupportRequestId,
    Guid? LinkedEventItemOccurrenceId,
    Guid? LinkedLmsClassId,
    string? ExternalReferenceKey,
    Guid? HostModeratorPersonId,
    int MaxParticipants,
    int EmptyTimeoutSeconds,
    bool AutoRecordAttendance,
    int MinimumAttendanceDwellSeconds,
    bool AutoStartRecordingEgress,
    bool IsWebinarBroadcast,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);
