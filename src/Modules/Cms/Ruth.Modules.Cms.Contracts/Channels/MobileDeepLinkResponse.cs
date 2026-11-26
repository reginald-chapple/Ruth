namespace Ruth.Modules.Cms.Contracts.Channels;

/// <summary>Represents a universal/app deep link routing rule for a MobileApplication.</summary>
public sealed record MobileDeepLinkResponse(
    Guid Id,
    Guid MobileApplicationId,
    string RoutePrefix,
    Guid? TargetMobilePageId,
    string FallbackUrl,
    bool UsesUniversalLinks,
    bool IsActive
);
