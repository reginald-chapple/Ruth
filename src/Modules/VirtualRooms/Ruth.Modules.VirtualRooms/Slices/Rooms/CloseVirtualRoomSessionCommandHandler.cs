using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class CloseVirtualRoomSessionCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    VirtualRoomAttendanceCollaborator attendanceCollaborator,
    IClock clock
) : ICommandHandler<CloseVirtualRoomSessionCommand, VirtualRoomSessionResponse>
{
    public async Task<Result<VirtualRoomSessionResponse>> HandleAsync(
        CloseVirtualRoomSessionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sessionId = VirtualRoomSessionId.From(command.SessionId);
        var session = await dbContext
            .VirtualRoomSessions.AsSplitQuery()
            .Include(static s => s.Participants)
            .Include(static s => s.Recordings)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<VirtualRoomSessionResponse>(VirtualRoomsErrors.SessionNotFound);
        }

        var room = await dbContext
            .VirtualRooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == session.VirtualRoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<VirtualRoomSessionResponse>(VirtualRoomsErrors.RoomNotFound);
        }

        if (!session.IsClosed)
        {
            var newlyQualified = session.CloseSession(
                room.MinimumAttendanceDwellSeconds,
                clock.UtcNow
            );
            foreach (var p in newlyQualified)
            {
                _ = await attendanceCollaborator.TryRecordQualifiedAttendanceAsync(
                    room,
                    session,
                    p,
                    cancellationToken
                );
            }

            _ = await providerSelector
                .ResolveActive()
                .CloseRoomAsync(room.LiveKitRoomName, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(VirtualRoomsProjectionMapper.MapSession(session, room));
    }
}
