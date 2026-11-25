using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Finds locations whose PostGIS geography point lies within the given radius in meters.</summary>
public sealed record FindLocationsWithinRadiusQuery(
    double Latitude,
    double Longitude,
    double RadiusMeters,
    bool ActiveOnly = true
) : IQuery<IReadOnlyList<LocationDistanceResponse>>;
