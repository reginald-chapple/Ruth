using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Finds locations whose PostGIS geofence polygon covers the given WGS84 coordinate.</summary>
public sealed record FindLocationsContainingPointQuery(
    double Latitude,
    double Longitude,
    bool ActiveOnly = true
) : IQuery<IReadOnlyList<LocationResponse>>;
