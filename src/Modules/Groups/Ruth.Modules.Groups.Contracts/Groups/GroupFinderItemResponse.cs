namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>PostGIS GroupFinder search result including geodesic distance and map pin.</summary>
public sealed record GroupFinderItemResponse(
    Guid GroupId,
    Guid GroupTypeId,
    string GroupTypeName,
    string Name,
    string? Description,
    Guid? CampusId,
    Guid? ScheduleId,
    MeetingStyle? MeetingStyle,
    int ActiveMemberCount,
    int? GroupCapacity,
    int? RemainingCapacity,
    bool HasOpenCapacity,
    Guid? PrimaryLocationId,
    string? LocationName,
    GroupGeoPointDto? GeoPoint,
    double? DistanceMeters,
    double? DistanceMiles,
    bool MatchedByGeofence,
    IReadOnlyDictionary<string, string?> Attributes,
    IReadOnlyDictionary<string, string?> Demographics
);
