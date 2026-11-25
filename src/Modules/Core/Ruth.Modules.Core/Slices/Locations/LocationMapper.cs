using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Locations;

internal static class LocationMapper
{
    public static LocationResponse ToResponse(Location location)
    {
        return new LocationResponse(
            location.Id.Value,
            location.SystemGuid,
            location.ParentLocationId?.Value,
            location.Name,
            location.GetDisplayName(),
            location.Description,
            location.IsNamedLocation,
            location.IsActive,
            location.LocationTypeValueId,
            location.ImageId,
            new LocationAddressDto(
                location.Street1,
                location.Street2,
                location.City,
                location.County,
                location.State,
                location.PostalCode,
                location.Country
            ),
            GeoGeometryHelper.ToPointDto(location.GeoPoint),
            GeoGeometryHelper.ToRingDto(location.GeoFence),
            location.IsGeoPointLocked,
            location.Barcode,
            location.AssessorParcelId,
            location.PrinterDeviceId?.Value,
            location.SoftRoomThreshold,
            location.FirmRoomThreshold,
            location.BeaconId,
            location.StandardizedOnUtc,
            location.GeocodedOnUtc,
            location.StandardizeAttemptedResult,
            location.GeocodeAttemptedResult,
            location.CreatedOnUtc,
            location.ModifiedOnUtc
        );
    }

    public static LocationInput ToInput(CreateLocationCommand cmd)
    {
        return new LocationInput(
            cmd.Name,
            cmd.ParentLocationId.HasValue ? LocationId.From(cmd.ParentLocationId.Value) : null,
            cmd.Description,
            cmd.IsActive,
            cmd.LocationTypeValueId,
            cmd.ImageId,
            cmd.Address,
            cmd.GeoPoint,
            cmd.GeoFenceRing,
            cmd.IsGeoPointLocked,
            cmd.Barcode,
            cmd.AssessorParcelId,
            cmd.PrinterDeviceId.HasValue ? DeviceId.From(cmd.PrinterDeviceId.Value) : null,
            cmd.SoftRoomThreshold,
            cmd.FirmRoomThreshold,
            cmd.BeaconId
        );
    }

    public static LocationInput ToInput(UpdateLocationCommand cmd, LocationId? existingParentId)
    {
        return new LocationInput(
            cmd.Name,
            existingParentId,
            cmd.Description,
            cmd.IsActive,
            cmd.LocationTypeValueId,
            cmd.ImageId,
            cmd.Address,
            cmd.GeoPoint,
            cmd.GeoFenceRing,
            cmd.IsGeoPointLocked,
            cmd.Barcode,
            cmd.AssessorParcelId,
            cmd.PrinterDeviceId.HasValue ? DeviceId.From(cmd.PrinterDeviceId.Value) : null,
            cmd.SoftRoomThreshold,
            cmd.FirmRoomThreshold,
            cmd.BeaconId
        );
    }
}
