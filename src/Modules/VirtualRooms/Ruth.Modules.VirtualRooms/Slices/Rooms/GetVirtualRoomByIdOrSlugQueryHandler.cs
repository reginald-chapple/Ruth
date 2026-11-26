using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class GetVirtualRoomByIdOrSlugQueryHandler(VirtualRoomsDbContext dbContext)
    : IQueryHandler<GetVirtualRoomByIdOrSlugQuery, VirtualRoomResponse>
{
    public async Task<Result<VirtualRoomResponse>> HandleAsync(
        GetVirtualRoomByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomNotFound);
        }

        var slugCandidate = query.IdOrSlug.Trim().ToLowerInvariant();
        var entity = Guid.TryParse(query.IdOrSlug, out var parsed)
            ? await dbContext
                .VirtualRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == VirtualRoomId.From(parsed), cancellationToken)
            : await dbContext
                .VirtualRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Slug == slugCandidate, cancellationToken);

        return entity is null
            ? Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomNotFound)
            : Result.Success(VirtualRoomsProjectionMapper.MapRoom(entity));
    }
}
