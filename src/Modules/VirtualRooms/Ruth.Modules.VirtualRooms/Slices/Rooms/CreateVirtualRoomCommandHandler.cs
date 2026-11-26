using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class CreateVirtualRoomCommandHandler(VirtualRoomsDbContext dbContext)
    : ICommandHandler<CreateVirtualRoomCommand, VirtualRoomResponse>
{
    public async Task<Result<VirtualRoomResponse>> HandleAsync(
        CreateVirtualRoomCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.Spec is null)
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomInvalid);
        }

        if (!string.IsNullOrWhiteSpace(command.Spec.ExternalReferenceKey))
        {
            var refKey = command.Spec.ExternalReferenceKey.Trim();
            var existingByRef = await dbContext
                .VirtualRooms.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ExternalReferenceKey == refKey, cancellationToken);
            if (existingByRef is not null)
            {
                return Result.Success(VirtualRoomsProjectionMapper.MapRoom(existingByRef));
            }
        }

        var created = VirtualRoom.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<VirtualRoomResponse>(created.PrimaryError);
        }

        var slugExists = await dbContext.VirtualRooms.AnyAsync(
            r => r.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugExists)
        {
            return Result.Failure<VirtualRoomResponse>(VirtualRoomsErrors.RoomSlugConflict);
        }

        _ = dbContext.VirtualRooms.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(VirtualRoomsProjectionMapper.MapRoom(created.Value));
    }
}
