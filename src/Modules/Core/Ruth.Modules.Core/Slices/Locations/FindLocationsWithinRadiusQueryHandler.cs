using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class FindLocationsWithinRadiusQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<FindLocationsWithinRadiusQuery, IReadOnlyList<LocationDistanceResponse>>
{
    private const double MaxEarthHalfCircumferenceMeters = 20_000_000.0;

    public async Task<Result<IReadOnlyList<LocationDistanceResponse>>> HandleAsync(
        FindLocationsWithinRadiusQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (
            double.IsNaN(query.RadiusMeters)
            || double.IsInfinity(query.RadiusMeters)
            || query.RadiusMeters is <= 0 or > MaxEarthHalfCircumferenceMeters
        )
        {
            return CoreLocationErrors.RadiusInvalid;
        }

        var originResult = GeoGeometryHelper.CreatePoint(
            new GeoPointDto(query.Latitude, query.Longitude)
        );
        if (originResult.IsFailure || originResult.Value is null)
        {
            return Result.Failure<IReadOnlyList<LocationDistanceResponse>>(originResult.Errors);
        }

        var origin = originResult.Value;
        var radius = query.RadiusMeters;

        var baseQuery = dbContext
            .Locations.AsNoTracking()
            .Where(l => l.GeoPoint != null && l.GeoPoint.IsWithinDistance(origin, radius));

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static l => l.IsActive);
        }

        var matches = await baseQuery
            .Select(l => new { Location = l, Distance = l.GeoPoint!.Distance(origin) })
            .OrderBy(static x => x.Distance)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<LocationDistanceResponse>>([
            .. matches.Select(static x => new LocationDistanceResponse(
                LocationMapper.ToResponse(x.Location),
                x.Distance
            )),
        ]);
    }
}
