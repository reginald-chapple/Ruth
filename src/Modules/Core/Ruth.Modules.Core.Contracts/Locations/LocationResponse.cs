namespace Ruth.Modules.Core.Contracts.Locations;

/// <summary>Detailed representation of a physical, named, or street-address location.</summary>
public sealed record LocationResponse(
    Guid Id,
    Guid SystemGuid,
    Guid? ParentLocationId,
    string? Name,
    string DisplayName,
    string? Description,
    bool IsNamedLocation,
    bool IsActive,
    Guid? LocationTypeValueId,
    Guid? ImageId,
    LocationAddressDto Address,
    GeoPointDto? GeoPoint,
    IReadOnlyList<GeoPointDto>? GeoFenceRing,
    bool IsGeoPointLocked,
    string? Barcode,
    string? AssessorParcelId,
    Guid? PrinterDeviceId,
    int? SoftRoomThreshold,
    int? FirmRoomThreshold,
    int? BeaconId,
    DateTimeOffset? StandardizedDateTimeUtc,
    DateTimeOffset? GeocodedDateTimeUtc,
    string? StandardizeAttemptedResult,
    string? GeocodeAttemptedResult,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
