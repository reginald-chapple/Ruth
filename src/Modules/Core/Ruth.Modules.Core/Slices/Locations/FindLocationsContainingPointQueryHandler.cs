using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class FindLocationsContainingPointQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<FindLocationsContainingPointQuery, IReadOnlyList<LocationResponse>>
{
    public async Task<Result<IReadOnlyList<LocationResponse>>> HandleAsync(
        FindLocationsContainingPointQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var pointResult = GeoGeometryHelper.CreatePoint(
            new GeoPointDto(query.Latitude, query.Longitude)
        );
        if (pointResult.IsFailure || pointResult.Value is null)
        {
            return Result.Failure<IReadOnlyList<LocationResponse>>(pointResult.Errors);
        }

        var target = pointResult.Value;
        var baseQuery = dbContext
            .Locations.AsNoTracking()
            .Where(l => l.GeoFence != null && l.GeoFence.Covers(target));

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static l => l.IsActive);
        }

        var locations = await baseQuery.OrderBy(static l => l.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<LocationResponse>>([
            .. locations.Select(LocationMapper.ToResponse),
        ]);
    }
}
