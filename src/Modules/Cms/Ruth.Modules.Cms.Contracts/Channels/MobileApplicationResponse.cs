namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Represents a configured native mobile shell application.</summary>
public sealed record MobileApplicationResponse(
    Guid Id,
    Guid? SiteId,
    string Name,
    string Slug,
    string BundleIdentifier,
    string ApiVersion,
    string MinimumClientVersion,
    string LatestClientVersion,
    MobileNavigationType NavigationType,
    string PrimaryColorHex,
    string SecondaryColorHex,
    IReadOnlyDictionary<string, string?> AdditionalSettings,
    bool IsActive,
    IReadOnlyList<MobileLayoutResponse> Layouts,
    IReadOnlyList<MobilePageResponse> Pages,
    IReadOnlyList<MobileDeepLinkResponse> DeepLinks
);
