using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class GetLocationByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetLocationByIdQuery, LocationResponse>
{
    public async Task<Result<LocationResponse>> HandleAsync(
        GetLocationByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = LocationId.From(query.LocationId);
        var location = await dbContext
            .Locations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return location is null
            ? CoreLocationErrors.LocationNotFound
            : Result.Success(LocationMapper.ToResponse(location));
    }
}
