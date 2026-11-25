namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Detailed representation of a physical kiosk, label printer, or check-in tablet.</summary>
public sealed record DeviceResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string? Description,
    Guid DeviceTypeValueId,
    string? IpAddress,
    bool IsActive,
    Guid? LocationId,
    DevicePrintFrom PrintFrom,
    DevicePrintToOverride PrintToOverride,
    Guid? PrinterDeviceId,
    Guid? ProxyDeviceId,
    bool HasCamera,
    CameraBarcodeConfiguration? CameraBarcodeConfigurationType,
    KioskType? KioskType,
    IReadOnlyList<Guid> AssignedLocationIds,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
