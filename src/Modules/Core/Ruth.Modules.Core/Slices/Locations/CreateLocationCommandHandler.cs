using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class CreateLocationCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateLocationCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> HandleAsync(
        CreateLocationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Location.Create(LocationMapper.ToInput(command));
        if (created.IsFailure)
        {
            return Result.Failure<LocationResponse>(created.Errors);
        }

        if (created.Value.ParentLocationId.HasValue)
        {
            var parentId = created.Value.ParentLocationId.Value;
            var parentExists = await dbContext.Locations.AnyAsync(
                l => l.Id == parentId,
                cancellationToken
            );
            if (!parentExists)
            {
                return CoreLocationErrors.LocationNotFound;
            }
        }

        _ = dbContext.Locations.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(LocationMapper.ToResponse(created.Value));
    }
}
