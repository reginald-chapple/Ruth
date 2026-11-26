using Ruth.Modules.VirtualRooms.Contracts.Providers;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Read projection of a participant's dwell and auto-attendance state within a VirtualRoomSession.</summary>
/// <param name="Id">Participant session row identifier.</param>
/// <param name="ParticipantIdentity">LiveKit participant identity.</param>
/// <param name="PersonId">Optional CRM Person identifier.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="Role">Participant role.</param>
/// <param name="IsCurrentlyConnected">Whether currently in the room.</param>
/// <param name="JoinCount">Number of join intervals.</param>
/// <param name="CumulativeDwellSeconds">Accumulated dwell duration in seconds.</param>
/// <param name="QualifiedForAttendance">True once dwell met MinimumAttendanceDwellSeconds.</param>
/// <param name="AttendanceRecordedAtUtc">UTC timestamp when Group Attendance was recorded.</param>
/// <param name="FirstJoinedAtUtc">First join UTC timestamp.</param>
/// <param name="LastLeftAtUtc">Optional last leave UTC timestamp.</param>
public sealed record VirtualRoomParticipantAttendanceResponse(
    Guid Id,
    string ParticipantIdentity,
    Guid? PersonId,
    string DisplayName,
    VirtualParticipantRole Role,
    bool IsCurrentlyConnected,
    int JoinCount,
    int CumulativeDwellSeconds,
    bool QualifiedForAttendance,
    DateTimeOffset? AttendanceRecordedAtUtc,
    DateTimeOffset FirstJoinedAtUtc,
    DateTimeOffset? LastLeftAtUtc
);
