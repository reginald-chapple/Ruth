using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class GetVirtualRoomsQueryHandler(VirtualRoomsDbContext dbContext)
    : IQueryHandler<GetVirtualRoomsQuery, IReadOnlyList<VirtualRoomResponse>>
{
    public async Task<Result<IReadOnlyList<VirtualRoomResponse>>> HandleAsync(
        GetVirtualRoomsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.VirtualRooms.AsNoTracking().AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static r => r.IsActive);
        }

        if (query.ContextKind.HasValue)
        {
            q = q.Where(r => r.ContextKind == query.ContextKind.Value);
        }

        var items = await q.OrderBy(static r => r.Name).ToListAsync(cancellationToken);
        IReadOnlyList<VirtualRoomResponse> mapped =
        [
            .. items.Select(VirtualRoomsProjectionMapper.MapRoom),
        ];
        return Result.Success(mapped);
    }
}
