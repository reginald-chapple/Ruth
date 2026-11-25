using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Updates an existing location's details, address, thresholds, and PostGIS geometry.</summary>
public sealed record UpdateLocationCommand(
    Guid LocationId,
    string? Name = null,
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
