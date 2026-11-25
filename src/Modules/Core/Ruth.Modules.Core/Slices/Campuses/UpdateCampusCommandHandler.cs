using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class UpdateCampusCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UpdateCampusCommand, CampusResponse>
{
    public async Task<Result<CampusResponse>> HandleAsync(
        UpdateCampusCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = CampusId.From(command.CampusId);
        var campus = await dbContext
            .Campuses.Include(static c => c.Schedules)
            .Include(static c => c.Topics)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campus is null)
        {
            return CoreLocationErrors.CampusNotFound;
        }

        var updated = campus.Update(CampusMapper.ToInput(command));
        if (updated.IsFailure)
        {
            return Result.Failure<CampusResponse>(updated.Errors);
        }

        var duplicate = await dbContext.Campuses.AnyAsync(
            c => c.Id != id && c.NormalizedName == campus.NormalizedName,
            cancellationToken
        );
        if (duplicate)
        {
            return CoreLocationErrors.CampusDuplicateName;
        }

        if (campus.LocationId.HasValue)
        {
            var locationId = campus.LocationId.Value;
            var locationExists = await dbContext.Locations.AnyAsync(
                l => l.Id == locationId,
                cancellationToken
            );
            if (!locationExists)
            {
                return CoreLocationErrors.LocationNotFound;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CampusMapper.ToResponse(campus));
    }
}
