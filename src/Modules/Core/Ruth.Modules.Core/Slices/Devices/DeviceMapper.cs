using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Devices;

internal static class DeviceMapper
{
    public static DeviceResponse ToResponse(Device device)
    {
        return new DeviceResponse(
            device.Id.Value,
            device.SystemGuid,
            device.Name,
            device.Description,
            device.DeviceTypeValueId,
            device.IpAddress,
            device.IsActive,
            device.LocationId?.Value,
            device.PrintFrom,
            device.PrintToOverride,
            device.PrinterDeviceId?.Value,
            device.ProxyDeviceId?.Value,
            device.HasCamera,
            device.CameraBarcodeConfigurationType,
            device.KioskType,
            [.. device.Locations.Select(static l => l.LocationId.Value)],
            device.CreatedOnUtc,
            device.ModifiedOnUtc
        );
    }

    public static DeviceInput ToInput(CreateDeviceCommand cmd)
    {
        return new DeviceInput(
            cmd.Name,
            cmd.DeviceTypeValueId,
            cmd.Description,
            cmd.IpAddress,
            cmd.IsActive,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.PrintFrom,
            cmd.PrintToOverride,
            cmd.PrinterDeviceId.HasValue ? DeviceId.From(cmd.PrinterDeviceId.Value) : null,
            cmd.ProxyDeviceId.HasValue ? DeviceId.From(cmd.ProxyDeviceId.Value) : null,
            cmd.HasCamera,
            cmd.CameraBarcodeConfigurationType,
            cmd.KioskType,
            cmd.AssignedLocationIds?.Select(LocationId.From).ToList()
        );
    }

    public static DeviceInput ToInput(UpdateDeviceCommand cmd)
    {
        return new DeviceInput(
            cmd.Name,
            cmd.DeviceTypeValueId,
            cmd.Description,
            cmd.IpAddress,
            cmd.IsActive,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.PrintFrom,
            cmd.PrintToOverride,
            cmd.PrinterDeviceId.HasValue ? DeviceId.From(cmd.PrinterDeviceId.Value) : null,
            cmd.ProxyDeviceId.HasValue ? DeviceId.From(cmd.ProxyDeviceId.Value) : null,
            cmd.HasCamera,
            cmd.CameraBarcodeConfigurationType,
            cmd.KioskType,
            cmd.AssignedLocationIds?.Select(LocationId.From).ToList()
        );
    }
}
