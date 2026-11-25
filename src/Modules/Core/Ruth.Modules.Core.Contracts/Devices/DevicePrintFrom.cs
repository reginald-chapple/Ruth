namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Indicates whether check-in labels are printed from the client browser/app or server.</summary>
public enum DevicePrintFrom
{
    /// <summary>Labels are dispatched from the client device.</summary>
    Client = 0,

    /// <summary>Labels are dispatched from the server over TCP/proxy.</summary>
    Server = 1,
}
