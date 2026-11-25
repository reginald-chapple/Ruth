namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Physical client platform running a check-in kiosk.</summary>
public enum KioskType
{
    /// <summary>Apple iPad native kiosk application.</summary>
    IPad = 0,

    /// <summary>Windows desktop check-in application.</summary>
    WindowsApp = 1,

    /// <summary>Web browser kiosk mode.</summary>
    Browser = 2,
}
