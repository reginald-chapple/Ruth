using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain.Rooms;

namespace Ruth.Modules.VirtualRooms.Slices;

internal static class VirtualRoomsProjectionMapper
{
    public static VirtualRoomResponse MapRoom(VirtualRoom room)
    {
        return new VirtualRoomResponse(
            room.Id.Value,
            room.Name,
            room.Slug,
            room.LiveKitRoomName,
            room.ContextKind,
            room.Description,
            room.LinkedGroupId,
            room.LinkedLocationId,
            room.LinkedScheduleId,
            room.LinkedBranchId,
            room.LinkedBookingId,
            room.LinkedSupportRequestId,
            room.LinkedEventItemOccurrenceId,
            room.LinkedLmsClassId,
            room.ExternalReferenceKey,
            room.HostModeratorPersonId,
            room.MaxParticipants,
            room.EmptyTimeoutSeconds,
            room.AutoRecordAttendance,
            room.MinimumAttendanceDwellSeconds,
            room.AutoStartRecordingEgress,
            room.IsWebinarBroadcast,
            room.IsActive,
            room.CreatedOnUtc
        );
    }

    public static VirtualRoomSessionResponse MapSession(
        VirtualRoomSession session,
        VirtualRoom room
    )
    {
        return new VirtualRoomSessionResponse(
            session.Id.Value,
            session.VirtualRoomId.Value,
            room.Name,
            room.LiveKitRoomName,
            session.LiveKitRoomSid,
            session.Title,
            session.Status,
            session.ScheduledStartUtc,
            session.ScheduledEndUtc,
            session.ActualStartedAtUtc,
            session.EndedAtUtc,
            [
                .. session
                    .Participants.OrderBy(static p => p.FirstJoinedAtUtc)
                    .Select(MapParticipant),
            ],
            [
                .. session
                    .Recordings.OrderByDescending(static r => r.StartedAtUtc)
                    .Select(MapRecording),
            ]
        );
    }

    public static VirtualRoomParticipantAttendanceResponse MapParticipant(
        VirtualRoomParticipantSession p
    )
    {
        return new VirtualRoomParticipantAttendanceResponse(
            p.Id.Value,
            p.ParticipantIdentity,
            p.PersonId,
            p.DisplayName,
            p.Role,
            p.IsCurrentlyConnected,
            p.JoinCount,
            p.CumulativeDwellSeconds,
            p.QualifiedForAttendance,
            p.AttendanceRecordedAtUtc,
            p.FirstJoinedAtUtc,
            p.LastLeftAtUtc
        );
    }

    public static VirtualRoomRecordingArchiveResponse MapRecording(VirtualRoomRecordingArchive r)
    {
        return new VirtualRoomRecordingArchiveResponse(
            r.Id.Value,
            r.EgressId,
            r.Status,
            r.StorageBucket,
            r.StorageObjectKey,
            r.TranscriptObjectKey,
            r.ContentType,
            r.SizeBytes,
            r.DurationSeconds,
            r.StartedAtUtc,
            r.CompletedAtUtc,
            r.ErrorMessage
        );
    }
}
