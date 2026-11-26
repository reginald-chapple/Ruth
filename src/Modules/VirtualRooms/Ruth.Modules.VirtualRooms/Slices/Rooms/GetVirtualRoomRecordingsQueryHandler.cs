using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class GetVirtualRoomRecordingsQueryHandler(VirtualRoomsDbContext dbContext)
    : IQueryHandler<
        GetVirtualRoomRecordingsQuery,
        IReadOnlyList<VirtualRoomRecordingArchiveResponse>
    >
{
    public async Task<Result<IReadOnlyList<VirtualRoomRecordingArchiveResponse>>> HandleAsync(
        GetVirtualRoomRecordingsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var sessionsQuery = dbContext.VirtualRoomSessions.AsNoTracking().AsQueryable();
        if (query.VirtualRoomId.HasValue)
        {
            var roomId = VirtualRoomId.From(query.VirtualRoomId.Value);
            sessionsQuery = sessionsQuery.Where(s => s.VirtualRoomId == roomId);
        }

        var sessionIds = await sessionsQuery
            .Select(static s => s.Id)
            .ToListAsync(cancellationToken);

        var recordingsQuery = dbContext
            .VirtualRoomRecordingArchives.AsNoTracking()
            .Where(r => sessionIds.Contains(r.SessionId));

        if (query.CompletedOnly)
        {
            recordingsQuery = recordingsQuery.Where(static r =>
                r.Status == VirtualRecordingStatus.Completed
            );
        }

        var items = await recordingsQuery
            .OrderByDescending(static r => r.StartedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        IReadOnlyList<VirtualRoomRecordingArchiveResponse> mapped =
        [
            .. items.Select(VirtualRoomsProjectionMapper.MapRecording),
        ];
        return Result.Success(mapped);
    }
}
