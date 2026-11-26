using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class GetVirtualRoomSessionsQueryHandler(VirtualRoomsDbContext dbContext)
    : IQueryHandler<GetVirtualRoomSessionsQuery, IReadOnlyList<VirtualRoomSessionResponse>>
{
    public async Task<Result<IReadOnlyList<VirtualRoomSessionResponse>>> HandleAsync(
        GetVirtualRoomSessionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext
            .VirtualRoomSessions.AsNoTracking()
            .AsSplitQuery()
            .Include(static s => s.Participants)
            .Include(static s => s.Recordings)
            .AsQueryable();

        if (query.VirtualRoomId.HasValue)
        {
            var roomId = VirtualRoomId.From(query.VirtualRoomId.Value);
            q = q.Where(s => s.VirtualRoomId == roomId);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(s => s.Status == query.Status.Value);
        }

        var sessions = await q.OrderByDescending(static s => s.ScheduledStartUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        var rooms = await dbContext
            .VirtualRooms.AsNoTracking()
            .ToDictionaryAsync(static r => r.Id, cancellationToken);

        List<VirtualRoomSessionResponse> mapped = [];
        foreach (var s in sessions)
        {
            if (rooms.TryGetValue(s.VirtualRoomId, out var room))
            {
                mapped.Add(VirtualRoomsProjectionMapper.MapSession(s, room));
            }
        }

        return Result.Success<IReadOnlyList<VirtualRoomSessionResponse>>(mapped);
    }
}
