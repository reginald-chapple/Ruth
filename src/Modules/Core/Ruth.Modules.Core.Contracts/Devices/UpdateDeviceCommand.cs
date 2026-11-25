using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Updates an existing physical device and its assigned check-in locations.</summary>
public sealed record UpdateDeviceCommand(
    Guid DeviceId,
    string Name,
    Guid DeviceTypeValueId,
    string? Description = null,
    string? IpAddress = null,
    bool IsActive = true,
    Guid? LocationId = null,
    DevicePrintFrom PrintFrom = DevicePrintFrom.Client,
    DevicePrintToOverride PrintToOverride = DevicePrintToOverride.Default,
    Guid? PrinterDeviceId = null,
    Guid? ProxyDeviceId = null,
    bool HasCamera = false,
    CameraBarcodeConfiguration? CameraBarcodeConfigurationType = null,
    KioskType? KioskType = null,
    IReadOnlyList<Guid>? AssignedLocationIds = null
) : ICommand<DeviceResponse>;
