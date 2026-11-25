using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Matches an active physical device by IP address, hostname, or PostGIS location geofence.</summary>
public sealed record MatchDeviceQuery(
    Guid DeviceTypeValueId,
    string? IpAddress = null,
    double? Latitude = null,
    double? Longitude = null,
    bool SkipReverseDns = true
) : IQuery<DeviceResponse>;
