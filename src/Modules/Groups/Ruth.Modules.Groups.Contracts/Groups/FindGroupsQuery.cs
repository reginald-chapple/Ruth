using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>PostGIS spatial and attribute GroupFinder query supporting radius, polygon, geofence, and capacity filters.</summary>
public sealed record FindGroupsQuery(
    Guid? GroupTypeId = null,
    Guid? CampusId = null,
    MeetingStyle? MeetingStyle = null,
    string? SearchText = null,
    bool PublicOnly = true,
    bool ActiveOnly = true,
    bool HasOpenCapacityOnly = false,
    double? Latitude = null,
    double? Longitude = null,
    double? RadiusMeters = null,
    IReadOnlyList<GroupGeoPointDto>? PolygonRing = null,
    IReadOnlyDictionary<string, string>? AttributeFilters = null,
    IReadOnlyDictionary<string, string>? DemographicFilters = null,
    int MaxResults = 50
) : IQuery<IReadOnlyList<GroupFinderItemResponse>>;
