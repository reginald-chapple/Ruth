using Ruth.Modules.Core.Contracts.Devices;

namespace Ruth.Modules.Core.Domain;

internal sealed record DeviceInput(
    string Name,
    Guid DeviceTypeValueId,
    string? Description,
    string? IpAddress,
    bool IsActive,
    LocationId? LocationId,
    DevicePrintFrom PrintFrom,
    DevicePrintToOverride PrintToOverride,
    DeviceId? PrinterDeviceId,
    DeviceId? ProxyDeviceId,
    bool HasCamera,
    CameraBarcodeConfiguration? CameraBarcodeConfigurationType,
    KioskType? KioskType,
    IReadOnlyList<LocationId>? AssignedLocationIds
);
