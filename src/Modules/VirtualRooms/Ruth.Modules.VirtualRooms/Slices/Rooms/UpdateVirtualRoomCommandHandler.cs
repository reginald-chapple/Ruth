using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class UpdateVirtualRoomCommandHandler(VirtualRoomsDbContext dbContext)
    : ICommandHandler<UpdateVirtualRoomCommand, VirtualRoomResponse>
{
    public async Task<Result<VirtualRoomResponse>> HandleAsync(
        UpdateVirtualRoomCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.Spec is null)
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomInvalid);
        }

        var id = VirtualRoomId.From(command.RoomId);
        var entity = await dbContext.VirtualRooms.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomNotFound);
        }

        var upd = entity.Update(command.Spec);
        if (upd.IsFailure)
        {
            return Result.Failure<VirtualRoomResponse>(upd.PrimaryError);
        }

        var conflict = await dbContext.VirtualRooms.AnyAsync(
            r => r.Id != id && r.Slug == entity.Slug,
            cancellationToken
        );
        if (conflict)
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomSlugConflict);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(VirtualRoomsProjectionMapper.MapRoom(entity));
    }
}
