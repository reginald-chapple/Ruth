namespace Ruth.Modules.Core.Contracts.Devices;

/// <summary>Determines which printer target overrides the default routing rule.</summary>
public enum DevicePrintToOverride
{
    /// <summary>Use the group type's default printer rule.</summary>
    Default = 0,

    /// <summary>Always print to the printer assigned to the kiosk device.</summary>
    Kiosk = 1,

    /// <summary>Always print to the printer assigned to the destination room location.</summary>
    Location = 2,
}
