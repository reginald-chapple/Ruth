using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class DeleteLocationCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteLocationCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteLocationCommand command,
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

        var hasChildren = await dbContext.Locations.AnyAsync(
            l => l.ParentLocationId == id,
            cancellationToken
        );
        if (hasChildren)
        {
            return CoreLocationErrors.LocationHasChildren;
        }

        _ = dbContext.Locations.Remove(location);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
