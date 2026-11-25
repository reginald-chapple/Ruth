using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class UpdateLocationCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UpdateLocationCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> HandleAsync(
        UpdateLocationCommand command,
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

        var updated = location.Update(LocationMapper.ToInput(command, location.ParentLocationId));
        if (updated.IsFailure)
        {
            return Result.Failure<LocationResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(LocationMapper.ToResponse(location));
    }
}
