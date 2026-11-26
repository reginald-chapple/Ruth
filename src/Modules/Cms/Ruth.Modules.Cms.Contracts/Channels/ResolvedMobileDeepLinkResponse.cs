namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Result of resolving an inbound deep link path for a mobile application.</summary>
public sealed record ResolvedMobileDeepLinkResponse(
    bool Matched,
    Guid? TargetMobilePageId,
    string? TargetPageKey,
    string? TargetRoute,
    string FallbackUrl,
    bool UsesUniversalLinks
);
