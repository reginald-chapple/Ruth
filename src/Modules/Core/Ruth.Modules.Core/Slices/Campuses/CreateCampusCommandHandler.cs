using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class CreateCampusCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateCampusCommand, CampusResponse>
{
    public async Task<Result<CampusResponse>> HandleAsync(
        CreateCampusCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Campus.Create(CampusMapper.ToInput(command));
        if (created.IsFailure)
        {
            return Result.Failure<CampusResponse>(created.Errors);
        }

        var normalizedName = created.Value.NormalizedName;
        var exists = await dbContext.Campuses.AnyAsync(
            c => c.NormalizedName == normalizedName,
            cancellationToken
        );
        if (exists)
        {
            return CoreLocationErrors.CampusDuplicateName;
        }

        if (created.Value.LocationId.HasValue)
        {
            var locationId = created.Value.LocationId.Value;
            var locationExists = await dbContext.Locations.AnyAsync(
                l => l.Id == locationId,
                cancellationToken
            );
            if (!locationExists)
            {
                return CoreLocationErrors.LocationNotFound;
            }
        }

        _ = dbContext.Campuses.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CampusMapper.ToResponse(created.Value));
    }
}
