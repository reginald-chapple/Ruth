namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Read projection of a VirtualRoomSession occurrence including participant dwell and recording archives.</summary>
/// <param name="Id">Session identifier.</param>
/// <param name="VirtualRoomId">Owning VirtualRoom identifier.</param>
/// <param name="RoomName">Room display name.</param>
/// <param name="LiveKitRoomName">Canonical LiveKit room name.</param>
/// <param name="LiveKitRoomSid">Optional media server room SID.</param>
/// <param name="Title">Session title.</param>
/// <param name="Status">Session status.</param>
/// <param name="ScheduledStartUtc">Scheduled start UTC timestamp.</param>
/// <param name="ScheduledEndUtc">Scheduled end UTC timestamp.</param>
/// <param name="ActualStartedAtUtc">Optional actual start UTC timestamp.</param>
/// <param name="EndedAtUtc">Optional ended UTC timestamp.</param>
/// <param name="Participants">Participant dwell and auto-attendance rows.</param>
/// <param name="Recordings">Recording archive rows.</param>
public sealed record VirtualRoomSessionResponse(
    Guid Id,
    Guid VirtualRoomId,
    string RoomName,
    string LiveKitRoomName,
    string? LiveKitRoomSid,
    string Title,
    VirtualRoomSessionStatus Status,
    DateTimeOffset ScheduledStartUtc,
    DateTimeOffset ScheduledEndUtc,
    DateTimeOffset? ActualStartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    IReadOnlyList<VirtualRoomParticipantAttendanceResponse> Participants,
    IReadOnlyList<VirtualRoomRecordingArchiveResponse> Recordings
);
