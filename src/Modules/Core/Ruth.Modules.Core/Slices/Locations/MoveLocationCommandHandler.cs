using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class MoveLocationCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<MoveLocationCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> HandleAsync(
        MoveLocationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = LocationId.From(command.LocationId);
        var location = await dbContext.Locations.FirstOrDefaultAsync(
            l => l.Id == id,
            cancellationToken
        );
        if (location is null)
        {
            return CoreLocationErrors.LocationNotFound;
        }

        LocationId? newParentId = command.NewParentLocationId.HasValue
            ? LocationId.From(command.NewParentLocationId.Value)
            : null;

        if (newParentId.HasValue)
        {
            var parentMap = await dbContext
                .Locations.AsNoTracking()
                .ToDictionaryAsync(
                    static l => l.Id,
                    static l => l.ParentLocationId,
                    cancellationToken
                );

            if (!parentMap.ContainsKey(newParentId.Value))
            {
                return CoreLocationErrors.LocationNotFound;
            }

            var cycleCheck = LocationTreeValidator.ValidateParentMove(id, newParentId, parentMap);
            if (cycleCheck.IsFailure)
            {
                return Result.Failure<LocationResponse>(cycleCheck.Errors);
            }
        }

        var moveResult = location.MoveToParent(newParentId);
        if (moveResult.IsFailure)
        {
            return Result.Failure<LocationResponse>(moveResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(LocationMapper.ToResponse(location));
    }
}
