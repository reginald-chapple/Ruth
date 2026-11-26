using System.Globalization;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.VirtualRooms.Contracts.Events;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class VirtualRoomAttendanceCollaborator(
    VirtualRoomsDbContext dbContext,
    ICommandDispatcher commandDispatcher,
    IClock clock
)
{
    public async Task<bool> TryRecordQualifiedAttendanceAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        VirtualRoomParticipantSession participant,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !participant.QualifiedForAttendance
            || participant.AttendanceRecordedAtUtc.HasValue
            || !participant.PersonId.HasValue
        )
        {
            return false;
        }

        var now = clock.UtcNow;
        StageQualifiedEvent(room, session, participant, now);

        if (!room.AutoRecordAttendance || !room.LinkedGroupId.HasValue)
        {
            participant.MarkAttendanceRecorded(now);
            return true;
        }

        var attendanceRes = await DispatchGroupAttendanceAsync(
            room,
            session,
            participant,
            cancellationToken
        );
        if (attendanceRes)
        {
            participant.MarkAttendanceRecorded(now);
            return true;
        }

        return false;
    }

    private void StageQualifiedEvent(
        VirtualRoom room,
        VirtualRoomSession session,
        VirtualRoomParticipantSession participant,
        DateTimeOffset now
    )
    {
        dbContext.EnqueueIntegrationEvent(
            new VirtualRoomAttendanceQualifiedIntegrationEvent(
                Guid.CreateVersion7(now),
                now,
                room.Id.Value,
                session.Id.Value,
                participant.PersonId!.Value,
                room.LinkedGroupId,
                participant.CumulativeDwellSeconds
            )
        );
    }

    private async Task<bool> DispatchGroupAttendanceAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        VirtualRoomParticipantSession participant,
        CancellationToken ct
    )
    {
        var occurrenceDate = DateOnly.FromDateTime(
            (session.ActualStartedAtUtc ?? session.ScheduledStartUtc).UtcDateTime
        );
        var note = string.Create(
            CultureInfo.InvariantCulture,
            $"LiveKit auto-attendance ({participant.CumulativeDwellSeconds}s dwell in '{room.Name}')"
        );

        var res = await commandDispatcher.SendAsync<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse
        >(
            new RecordGroupAttendanceCommand(
                room.LinkedGroupId!.Value,
                occurrenceDate,
                LocationId: room.LinkedLocationId,
                ScheduleId: room.LinkedScheduleId,
                Name: session.Title,
                Entries:
                [
                    new GroupAttendanceEntryInputDto(
                        participant.PersonId!.Value,
                        DidAttend: true,
                        Note: note
                    ),
                ]
            ),
            ct
        );
        return res.IsSuccess;
    }
}
