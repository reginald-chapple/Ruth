using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class StartVirtualRoomRecordingCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    IClock clock
) : ICommandHandler<StartVirtualRoomRecordingCommand, VirtualRoomRecordingArchiveResponse>
{
    public async Task<Result<VirtualRoomRecordingArchiveResponse>> HandleAsync(
        StartVirtualRoomRecordingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sessionId = VirtualRoomSessionId.From(command.SessionId);
        var session = await dbContext
            .VirtualRoomSessions.Include(static s => s.Recordings)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<VirtualRoomRecordingArchiveResponse>(
                VirtualRoomsErrors.SessionNotFound
            );
        }

        var room = await dbContext
            .VirtualRooms.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == session.VirtualRoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<VirtualRoomRecordingArchiveResponse>(
                VirtualRoomsErrors.RoomNotFound
            );
        }

        var now = clock.UtcNow;
        var objectKey =
            $"virtual-rooms/recordings/{room.Id.Value:D}/{session.Id.Value:D}/{Guid.CreateVersion7(now):N}.mp4";
        var egressRes = await providerSelector
            .ResolveActive()
            .StartRoomCompositeEgressAsync(
                new VirtualRoomEgressSpec(
                    room.LiveKitRoomName,
                    providerSelector.DefaultStorageBucket,
                    objectKey,
                    string.IsNullOrWhiteSpace(command.Layout) ? "grid" : command.Layout.Trim()
                ),
                cancellationToken
            );
        if (egressRes.IsFailure)
        {
            return Result.Failure<VirtualRoomRecordingArchiveResponse>(egressRes.PrimaryError);
        }

        var archive = session.RegisterRecordingStart(
            egressRes.Value.EgressId,
            egressRes.Value.StorageBucket,
            egressRes.Value.StorageObjectKey,
            now
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(VirtualRoomsProjectionMapper.MapRecording(archive));
    }
}
