using Ruth.Modules.Groups.Contracts.Groups;

namespace Ruth.Modules.Groups.Slices.Groups;

internal sealed record GroupFinderSpatialMatch(
    Guid? LocationId,
    string? LocationName,
    GroupGeoPointDto? GeoPoint,
    double? DistanceMeters,
    bool MatchedByGeofence
);
