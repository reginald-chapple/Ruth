namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Camera barcode scanning mode for check-in kiosk tablets.</summary>
public enum CameraBarcodeConfiguration
{
    /// <summary>Camera barcode scanning is disabled.</summary>
    Off = 0,

    /// <summary>Camera scanning is available via an on-screen button.</summary>
    Available = 1,

    /// <summary>Camera scanner remains continuously active.</summary>
    AlwaysOn = 2,

    /// <summary>Camera listens passively in the background.</summary>
    Passive = 3,
}
