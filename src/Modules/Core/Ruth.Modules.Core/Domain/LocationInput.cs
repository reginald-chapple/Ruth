using Ruth.Modules.Core.Contracts.Locations;

namespace Ruth.Modules.Core.Domain;

internal sealed record LocationInput(
    string? Name,
    LocationId? ParentLocationId,
    string? Description,
    bool IsActive,
    Guid? LocationTypeValueId,
    Guid? ImageId,
    LocationAddressDto? Address,
    GeoPointDto? GeoPoint,
    IReadOnlyList<GeoPointDto>? GeoFenceRing,
    bool IsGeoPointLocked,
    string? Barcode,
    string? AssessorParcelId,
    DeviceId? PrinterDeviceId,
    int? SoftRoomThreshold,
    int? FirmRoomThreshold,
    int? BeaconId
);
