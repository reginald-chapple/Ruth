namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Location search result annotated with PostGIS spherical distance in meters.</summary>
public sealed record LocationDistanceResponse(LocationResponse Location, double DistanceMeters);
