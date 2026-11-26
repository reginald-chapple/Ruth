using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class StopVirtualRoomRecordingCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    IClock clock
) : ICommandHandler<StopVirtualRoomRecordingCommand, VirtualRoomRecordingArchiveResponse>
{
    public async Task<Result<VirtualRoomRecordingArchiveResponse>> HandleAsync(
        StopVirtualRoomRecordingCommand command,
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

        var recording = session.Recordings.FirstOrDefault(r =>
            string.Equals(r.EgressId, command.EgressId?.Trim(), StringComparison.OrdinalIgnoreCase)
        );
        if (recording is null)
        {
            return Result.Failure<VirtualRoomRecordingArchiveResponse>(
                VirtualRoomsErrors.RecordingNotFound
            );
        }

        var stopRes = await providerSelector
            .ResolveActive()
            .StopEgressAsync(recording.EgressId, cancellationToken);
        if (stopRes.IsFailure)
        {
            return Result.Failure<VirtualRoomRecordingArchiveResponse>(stopRes.PrimaryError);
        }

        var elapsed = (int)Math.Max(1, (clock.UtcNow - recording.StartedAtUtc).TotalSeconds);
        recording.MarkCompleted(
            recording.SizeBytes,
            elapsed,
            overrideObjectKey: null,
            clock.UtcNow
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(VirtualRoomsProjectionMapper.MapRecording(recording));
    }
}
