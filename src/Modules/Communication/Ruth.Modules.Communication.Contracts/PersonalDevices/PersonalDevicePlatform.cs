namespace Ruth.Modules.Communication.Contracts.PersonalDevices;

/// <summary>Client operating system or browser platform for registered personal push devices.</summary>
public enum PersonalDevicePlatform
{
    /// <summary>Apple iOS / iPadOS (APNs).</summary>
    Ios = 0,

    /// <summary>Google Android (FCM).</summary>
    Android = 1,

    /// <summary>W3C Web Push browser subscription.</summary>
    WebPush = 2,

    /// <summary>Desktop native client.</summary>
    Desktop = 3,
}
