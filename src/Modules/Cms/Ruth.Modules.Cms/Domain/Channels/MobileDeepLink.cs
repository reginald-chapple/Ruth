using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Universal Link / App Link routing rule for a <see cref="MobileApplication" />.</summary>
public sealed class MobileDeepLink : Entity<MobileDeepLinkId>
{
    private MobileDeepLink() { }

    private MobileDeepLink(
        MobileDeepLinkId id,
        MobileApplicationId mobileApplicationId,
        string routePrefix,
        MobilePageId? targetMobilePageId,
        string fallbackUrl,
        bool usesUniversalLinks
    )
        : base(id)
    {
        MobileApplicationId = mobileApplicationId;
        RoutePrefix = routePrefix;
        TargetMobilePageId = targetMobilePageId;
        FallbackUrl = fallbackUrl;
        UsesUniversalLinks = usesUniversalLinks;
        IsActive = true;
    }

    public MobileApplicationId MobileApplicationId { get; private set; }
    public string RoutePrefix { get; private set; } = string.Empty;
    public MobilePageId? TargetMobilePageId { get; private set; }
    public string FallbackUrl { get; private set; } = string.Empty;
    public bool UsesUniversalLinks { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public static Result<MobileDeepLink> Create(
        MobileApplicationId mobileApplicationId,
        string routePrefix,
        string fallbackUrl,
        MobilePageId? targetMobilePageId = null,
        bool usesUniversalLinks = true
    )
    {
        var normalizedPrefix = "/" + CmsTextNormalizer.NormalizeRoute(routePrefix);
        return mobileApplicationId.IsEmpty || normalizedPrefix.Length <= 1
                ? Result.Failure<MobileDeepLink>(
                    CmsErrors.Validation(
                        "MobileDeepLink.Invalid",
                        "MobileApplicationId and a non-root RoutePrefix are required."
                    )
                )
            : !CmsTextNormalizer.IsSafeHttpOrRelativeUrl(fallbackUrl)
                ? Result.Failure<MobileDeepLink>(
                    CmsErrors.Validation(
                        "MobileDeepLink.FallbackInvalid",
                        "FallbackUrl must be an http/https URL or root-relative path."
                    )
                )
            : Result.Success(
                new MobileDeepLink(
                    MobileDeepLinkId.Create(),
                    mobileApplicationId,
                    normalizedPrefix,
                    targetMobilePageId,
                    fallbackUrl.Trim(),
                    usesUniversalLinks
                )
            );
    }
}
