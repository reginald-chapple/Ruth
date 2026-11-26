namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Navigation shell style for a native mobile application.</summary>
public enum MobileNavigationType
{
    /// <summary>Bottom tab bar navigation shell.</summary>
    TabBar = 0,

    /// <summary>Side drawer flyout navigation shell.</summary>
    Flyout = 1,

    /// <summary>Single-screen kiosk or focused companion layout.</summary>
    SinglePage = 2,
}
