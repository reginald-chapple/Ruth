using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Native mobile shell application backend aggregate owning layouts, pages, and deep links.</summary>
public sealed class MobileApplication
    : AggregateRoot<MobileApplicationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Cms.MobileApplication";

    private readonly List<MobileLayout> _layouts = [];
    private readonly List<MobilePage> _pages = [];
    private readonly List<MobileDeepLink> _deepLinks = [];
    private Dictionary<string, string?> _additionalSettings = new(StringComparer.OrdinalIgnoreCase);

    private MobileApplication() { }

    private MobileApplication(MobileApplicationId id)
        : base(id) { }

    public CmsSiteId? SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string BundleIdentifier { get; private set; } = string.Empty;
    public string ApiVersion { get; private set; } = "1.0";
    public string MinimumClientVersion { get; private set; } = "1.0.0";
    public string LatestClientVersion { get; private set; } = "1.0.0";
    public MobileNavigationType NavigationType { get; private set; }
    public string PrimaryColorHex { get; private set; } = "#0F6CBD";
    public string SecondaryColorHex { get; private set; } = "#115EA3";
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<MobileLayout> Layouts => _layouts.AsReadOnly();
    public IReadOnlyCollection<MobilePage> Pages => _pages.AsReadOnly();
    public IReadOnlyCollection<MobileDeepLink> DeepLinks => _deepLinks.AsReadOnly();

    public IReadOnlyDictionary<string, string?> AdditionalSettings
    {
        get => _additionalSettings;
        private set =>
            _additionalSettings = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<MobileApplication> Create(
        string name,
        string slug,
        string bundleIdentifier,
        CmsSiteId? siteId = null,
        string apiVersion = "1.0",
        string minimumClientVersion = "1.0.0",
        string latestClientVersion = "1.0.0",
        MobileNavigationType navigationType = MobileNavigationType.TabBar,
        string primaryColorHex = "#0F6CBD",
        string secondaryColorHex = "#115EA3",
        IReadOnlyDictionary<string, string?>? additionalSettings = null
    )
    {
        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        return
            string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(normalizedSlug)
            || string.IsNullOrWhiteSpace(bundleIdentifier)
            ? Result.Failure<MobileApplication>(
                CmsErrors.Validation(
                    "MobileApplication.Invalid",
                    "Name, Slug, and BundleIdentifier are required."
                )
            )
            : Result.Success(
                new MobileApplication(MobileApplicationId.Create())
                {
                    SiteId = siteId,
                    Name = name.Trim(),
                    Slug = normalizedSlug,
                    BundleIdentifier = bundleIdentifier.Trim(),
                    ApiVersion = string.IsNullOrWhiteSpace(apiVersion) ? "1.0" : apiVersion.Trim(),
                    MinimumClientVersion = string.IsNullOrWhiteSpace(minimumClientVersion)
                        ? "1.0.0"
                        : minimumClientVersion.Trim(),
                    LatestClientVersion = string.IsNullOrWhiteSpace(latestClientVersion)
                        ? "1.0.0"
                        : latestClientVersion.Trim(),
                    NavigationType = navigationType,
                    PrimaryColorHex = string.IsNullOrWhiteSpace(primaryColorHex)
                        ? "#0F6CBD"
                        : primaryColorHex.Trim(),
                    SecondaryColorHex = string.IsNullOrWhiteSpace(secondaryColorHex)
                        ? "#115EA3"
                        : secondaryColorHex.Trim(),
                    IsActive = true,
                    _additionalSettings = additionalSettings is null
                        ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        : new Dictionary<string, string?>(
                            additionalSettings,
                            StringComparer.OrdinalIgnoreCase
                        ),
                }
            );
    }

    public Result<MobileLayout> AddLayout(
        string name,
        string layoutKey,
        string shellMarkup,
        int order = 0
    )
    {
        var res = MobileLayout.Create(Id, name, layoutKey, shellMarkup, order);
        if (res.IsFailure)
        {
            return res;
        }

        if (
            _layouts.Exists(l =>
                string.Equals(l.LayoutKey, res.Value.LayoutKey, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Result.Failure<MobileLayout>(
                CmsErrors.Conflict(
                    "MobileLayout.Duplicate",
                    $"Layout key '{res.Value.LayoutKey}' already exists."
                )
            );
        }

        _layouts.Add(res.Value);
        return res;
    }

    public Result<MobilePage> AddPage(
        string title,
        string pageKey,
        string route,
        MobileLayoutId? layoutId = null,
        string? iconName = null,
        int order = 0,
        bool showInNavigation = true,
        bool requiresAuthentication = false,
        ContentChannelId? contentChannelId = null,
        string? adaptiveMessageKey = null,
        string blocksJson = "[]"
    )
    {
        var res = MobilePage.Create(
            Id,
            title,
            pageKey,
            route,
            layoutId,
            iconName,
            order,
            showInNavigation,
            requiresAuthentication,
            contentChannelId,
            adaptiveMessageKey,
            blocksJson
        );
        if (res.IsFailure)
        {
            return res;
        }

        if (
            _pages.Exists(p =>
                string.Equals(p.PageKey, res.Value.PageKey, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Result.Failure<MobilePage>(
                CmsErrors.Conflict(
                    "MobilePage.Duplicate",
                    $"Page key '{res.Value.PageKey}' already exists."
                )
            );
        }

        _pages.Add(res.Value);
        return res;
    }

    public Result<MobileDeepLink> AddDeepLink(
        string routePrefix,
        string fallbackUrl,
        MobilePageId? targetPageId = null,
        bool usesUniversalLinks = true
    )
    {
        var res = MobileDeepLink.Create(
            Id,
            routePrefix,
            fallbackUrl,
            targetPageId,
            usesUniversalLinks
        );
        if (res.IsFailure)
        {
            return res;
        }

        _deepLinks.Add(res.Value);
        return res;
    }

    public bool IsClientUpgradeRequired(string? clientVersion)
    {
        return !string.IsNullOrWhiteSpace(clientVersion)
            && Version.TryParse(clientVersion.Trim(), out var current)
            && Version.TryParse(MinimumClientVersion, out var min)
            && current < min;
    }

    public ResolvedMobileDeepLinkResponse ResolveDeepLink(string rawPath)
    {
        var normalized = "/" + CmsTextNormalizer.NormalizeRoute(rawPath);
        var matched = _deepLinks
            .Where(static d => d.IsActive)
            .OrderByDescending(static d => d.RoutePrefix.Length)
            .FirstOrDefault(d =>
                normalized.StartsWith(d.RoutePrefix, StringComparison.OrdinalIgnoreCase)
            );

        if (matched is null)
        {
            return new ResolvedMobileDeepLinkResponse(
                Matched: false,
                TargetMobilePageId: null,
                TargetPageKey: null,
                TargetRoute: null,
                "/",
                UsesUniversalLinks: false
            );
        }

        var targetPage = matched.TargetMobilePageId is not null
            ? _pages.FirstOrDefault(p => p.Id == matched.TargetMobilePageId.Value)
            : null;

        return new ResolvedMobileDeepLinkResponse(
            Matched: true,
            targetPage?.Id.Value,
            targetPage?.PageKey,
            targetPage?.Route,
            matched.FallbackUrl,
            matched.UsesUniversalLinks
        );
    }
}
