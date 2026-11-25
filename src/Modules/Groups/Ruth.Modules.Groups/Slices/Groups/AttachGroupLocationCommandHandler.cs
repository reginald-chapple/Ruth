using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed class AttachGroupLocationCommandHandler(
    GroupsDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<AttachGroupLocationCommand, GroupDetailResponse>
{
    public async Task<Result<GroupDetailResponse>> HandleAsync(
        AttachGroupLocationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var groupId = GroupId.From(command.GroupId);
        var group = await dbContext
            .Groups.Include(static g => g.Locations)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            return GroupsErrors.GroupNotFound;
        }

        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .FirstOrDefaultAsync(gt => gt.Id == group.GroupTypeId, cancellationToken);
        if (groupType is null)
        {
            return GroupsErrors.GroupTypeNotFound;
        }

        var (resolvedName, resolvedPoint, resolvedFence) = await ResolveCoreLocationMetadataAsync(
            command,
            cancellationToken
        );

        var attached = group.AttachOrUpdateLocation(
            groupType,
            command.LocationId,
            resolvedName,
            command.GroupLocationTypeValueId,
            command.IsMailingLocation,
            command.IsMappedLocation,
            command.IsOverflowLocation,
            command.GroupMemberPersonId,
            command.Order,
            resolvedPoint,
            resolvedFence,
            command.ScheduleIds
        );
        if (attached.IsFailure)
        {
            return Result.Failure<GroupDetailResponse>(attached.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            await GroupMapper.LoadDetailResponseAsync(
                dbContext,
                group,
                groupType.Name,
                cancellationToken
            )
        );
    }

    private async Task<(
        string? Name,
        GroupGeoPointDto? GeoPoint,
        IReadOnlyList<GroupGeoPointDto>? GeoFence
    )> ResolveCoreLocationMetadataAsync(
        AttachGroupLocationCommand command,
        CancellationToken cancellationToken
    )
    {
        var name = command.LocationName;
        var point = command.GeoPoint;
        var fence = command.GeoFenceRing;

        if (!string.IsNullOrWhiteSpace(name) && point is not null)
        {
            return (name, point, fence);
        }

        var coreLocation = await queryDispatcher.QueryAsync<GetLocationByIdQuery, LocationResponse>(
            new GetLocationByIdQuery(command.LocationId),
            cancellationToken
        );
        if (!coreLocation.IsSuccess)
        {
            return (name, point, fence);
        }

        var loc = coreLocation.Value;
        name = string.IsNullOrWhiteSpace(name) ? loc.DisplayName : name;
        point ??= loc.GeoPoint is null
            ? null
            : new GroupGeoPointDto(loc.GeoPoint.Latitude, loc.GeoPoint.Longitude);
        fence ??= loc.GeoFenceRing is null
            ? null
            :
            [
                .. loc.GeoFenceRing.Select(static p => new GroupGeoPointDto(
                    p.Latitude,
                    p.Longitude
                )),
            ];
        return (name, point, fence);
    }
}
