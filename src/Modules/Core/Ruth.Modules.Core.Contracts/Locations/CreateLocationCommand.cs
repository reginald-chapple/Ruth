using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Creates a named location, building/room, geofence, or street address.</summary>
public sealed record CreateLocationCommand(
    string? Name = null,
    Guid? ParentLocationId = null,
    string? Description = null,
    bool IsActive = true,
    Guid? LocationTypeValueId = null,
    Guid? ImageId = null,
    LocationAddressDto? Address = null,
    GeoPointDto? GeoPoint = null,
    IReadOnlyList<GeoPointDto>? GeoFenceRing = null,
    bool IsGeoPointLocked = false,
    string? Barcode = null,
    string? AssessorParcelId = null,
    Guid? PrinterDeviceId = null,
    int? SoftRoomThreshold = null,
    int? FirmRoomThreshold = null,
    int? BeaconId = null
) : ICommand<LocationResponse>;
