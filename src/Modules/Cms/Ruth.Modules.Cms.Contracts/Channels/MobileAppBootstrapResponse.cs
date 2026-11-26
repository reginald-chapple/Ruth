namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Versioned bootstrap payload returned to native mobile application shells.</summary>
public sealed record MobileAppBootstrapResponse(
    string ContractVersion,
    Guid ApplicationId,
    string Slug,
    string Name,
    string BundleIdentifier,
    string MinimumClientVersion,
    string LatestClientVersion,
    bool ClientUpgradeRequired,
    MobileNavigationType NavigationType,
    string PrimaryColorHex,
    string SecondaryColorHex,
    IReadOnlyList<MobileLayoutResponse> Layouts,
    IReadOnlyList<MobilePageResponse> NavigationPages,
    IReadOnlyList<MobileDeepLinkResponse> DeepLinks,
    DateTimeOffset GeneratedAtUtc
);
