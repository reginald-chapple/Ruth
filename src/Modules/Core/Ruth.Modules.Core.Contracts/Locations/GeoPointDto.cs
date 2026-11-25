namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>WGS84 geographic coordinate pair (latitude, longitude).</summary>
public sealed record GeoPointDto(double Latitude, double Longitude);
