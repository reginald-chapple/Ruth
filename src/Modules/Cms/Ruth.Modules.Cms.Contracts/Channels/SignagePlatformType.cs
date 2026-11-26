namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Target display platform for a digital signage / TV feed application.</summary>
public enum SignagePlatformType
{
    /// <summary>Generic web or kiosk digital signage display.</summary>
    DigitalSignage = 0,

    /// <summary>Apple TV (tvOS) application showcase feed.</summary>
    AppleTv = 1,

    /// <summary>Roku Direct Publisher JSON feed.</summary>
    Roku = 2,
}
