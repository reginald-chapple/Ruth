using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class CreateVirtualRoomSessionCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    IClock clock
) : ICommandHandler<CreateVirtualRoomSessionCommand, VirtualRoomSessionResponse>
{
    public async Task<Result<VirtualRoomSessionResponse>> HandleAsync(
        CreateVirtualRoomSessionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var roomId = VirtualRoomId.From(command.VirtualRoomId);
        var room = await dbContext.VirtualRooms.FirstOrDefaultAsync(
            r => r.Id == roomId,
            cancellationToken
        );
        if (room is null)
        {
            return Result.Failure<VirtualRoomSessionResponse>(VirtualRoomsErrors.RoomNotFound);
        }

        var sessionRes = VirtualRoomSession.Create(
            room,
            command.Title,
            command.ScheduledStartUtc,
            command.ScheduledEndUtc
        );
        if (sessionRes.IsFailure)
        {
            return Result.Failure<VirtualRoomSessionResponse>(sessionRes.PrimaryError);
        }

        var session = sessionRes.Value;
        if (command.StartLiveNow)
        {
            var startErr = await ProvisionLiveAndOptionalEgressAsync(
                room,
                session,
                cancellationToken
            );
            if (startErr is not null)
            {
                return Result.Failure<VirtualRoomSessionResponse>(startErr);
            }
        }

        _ = dbContext.VirtualRoomSessions.Add(session);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(VirtualRoomsProjectionMapper.MapSession(session, room));
    }

    private async Task<DomainError?> ProvisionLiveAndOptionalEgressAsync(
        VirtualRoom room,
        VirtualRoomSession session,
        CancellationToken ct
    )
    {
        var provider = providerSelector.ResolveActive();
        var provisionRes = await provider.EnsureRoomAsync(
            new VirtualRoomProvisionSpec(
                room.LiveKitRoomName,
                room.MaxParticipants,
                room.EmptyTimeoutSeconds
            ),
            ct
        );
        if (provisionRes.IsFailure)
        {
            return provisionRes.PrimaryError;
        }

        var liveRes = session.MarkLive(provisionRes.Value.LiveKitRoomSid, clock.UtcNow);
        if (liveRes.IsFailure)
        {
            return liveRes.PrimaryError;
        }

        if (room.AutoStartRecordingEgress)
        {
            var objKey =
                $"virtual-rooms/recordings/{room.Id.Value:D}/{session.Id.Value:D}/composite.mp4";
            var egressRes = await provider.StartRoomCompositeEgressAsync(
                new VirtualRoomEgressSpec(
                    room.LiveKitRoomName,
                    providerSelector.DefaultStorageBucket,
                    objKey
                ),
                ct
            );
            if (egressRes.IsSuccess)
            {
                _ = session.RegisterRecordingStart(
                    egressRes.Value.EgressId,
                    egressRes.Value.StorageBucket,
                    egressRes.Value.StorageObjectKey,
                    clock.UtcNow
                );
            }
        }

        return null;
    }
}
