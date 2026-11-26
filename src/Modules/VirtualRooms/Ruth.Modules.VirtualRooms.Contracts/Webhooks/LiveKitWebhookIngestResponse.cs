namespace Ruth.Modules.VirtualRooms.Contracts.Webhooks;

/// <summary>Outcome of processing a verified LiveKit webhook event.</summary>
/// <param name="EventType">LiveKit webhook event name.</param>
/// <param name="MatchedSessionId">Optional matched VirtualRoomSession identifier.</param>
/// <param name="AttendanceRecorded">True when Group Attendance was auto-recorded during this webhook.</param>
/// <param name="RecordingArchived">True when a RustFS recording archive reached Completed during this webhook.</param>
public sealed record LiveKitWebhookIngestResponse(
    string EventType,
    Guid? MatchedSessionId,
    bool AttendanceRecorded,
    bool RecordingArchived
);
