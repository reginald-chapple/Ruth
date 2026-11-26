namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Specification for creating or updating an embedded VirtualRoom.</summary>
/// <param name="Name">Human-readable room title.</param>
/// <param name="Slug">Unique URL-safe room slug.</param>
/// <param name="ContextKind">Linked Ruth domain context kind.</param>
/// <param name="Description">Optional description or agenda.</param>
/// <param name="LinkedGroupId">Optional Groups module Group identifier (used for auto-attendance).</param>
/// <param name="LinkedLocationId">Optional Core Location identifier.</param>
/// <param name="LinkedScheduleId">Optional Core Schedule identifier.</param>
/// <param name="LinkedBranchId">Optional Branch/Site identifier.</param>
/// <param name="LinkedBookingId">Optional Commerce booking identifier.</param>
/// <param name="LinkedSupportRequestId">Optional Engagement SupportRequest identifier.</param>
/// <param name="LinkedEventItemOccurrenceId">Optional Events occurrence identifier.</param>
/// <param name="LinkedLmsClassId">Optional LMS class identifier.</param>
/// <param name="ExternalReferenceKey">Optional idempotent correlation key.</param>
/// <param name="HostModeratorPersonId">Optional designated host/moderator CRM Person identifier.</param>
/// <param name="MaxParticipants">Maximum simultaneous participants (2..2000).</param>
/// <param name="EmptyTimeoutSeconds">Idle room close timeout in seconds.</param>
/// <param name="AutoRecordAttendance">Automatically records Group Attendance once dwell threshold is met.</param>
/// <param name="MinimumAttendanceDwellSeconds">Minimum cumulative participant dwell seconds to qualify for attendance.</param>
/// <param name="AutoStartRecordingEgress">Automatically starts RustFS recording egress when session goes live.</param>
/// <param name="IsWebinarBroadcast">When true, non-moderators join as view-only Observers.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record VirtualRoomSpec(
    string Name,
    string Slug,
    VirtualRoomContextKind ContextKind = VirtualRoomContextKind.Standalone,
    string? Description = null,
    Guid? LinkedGroupId = null,
    Guid? LinkedLocationId = null,
    Guid? LinkedScheduleId = null,
    Guid? LinkedBranchId = null,
    Guid? LinkedBookingId = null,
    Guid? LinkedSupportRequestId = null,
    Guid? LinkedEventItemOccurrenceId = null,
    Guid? LinkedLmsClassId = null,
    string? ExternalReferenceKey = null,
    Guid? HostModeratorPersonId = null,
    int MaxParticipants = 100,
    int EmptyTimeoutSeconds = 300,
    bool AutoRecordAttendance = true,
    int MinimumAttendanceDwellSeconds = 180,
    bool AutoStartRecordingEgress = false,
    bool IsWebinarBroadcast = false,
    bool IsActive = true
);
