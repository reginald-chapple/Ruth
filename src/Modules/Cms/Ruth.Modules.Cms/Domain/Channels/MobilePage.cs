using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Configured screen/page inside a <see cref="MobileApplication" />.</summary>
public sealed class MobilePage : Entity<MobilePageId>
{
    private MobilePage() { }

    private MobilePage(MobilePageId id)
        : base(id) { }

    public MobileApplicationId MobileApplicationId { get; private set; }
    public MobileLayoutId? MobileLayoutId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string PageKey { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public string? IconName { get; private set; }
    public int Order { get; private set; }
    public bool ShowInNavigation { get; private set; } = true;
    public bool RequiresAuthentication { get; private set; }
    public ContentChannelId? ContentChannelId { get; private set; }
    public string? AdaptiveMessageKey { get; private set; }
    public string BlocksJson { get; private set; } = "[]";
    public bool IsActive { get; private set; } = true;

    public static Result<MobilePage> Create(
        MobileApplicationId mobileApplicationId,
        string title,
        string pageKey,
        string route,
        MobileLayoutId? mobileLayoutId = null,
        string? iconName = null,
        int order = 0,
        bool showInNavigation = true,
        bool requiresAuthentication = false,
        ContentChannelId? contentChannelId = null,
        string? adaptiveMessageKey = null,
        string blocksJson = "[]"
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(pageKey);
        var normalizedRoute = "/" + CmsTextNormalizer.NormalizeRoute(route);
        return
            mobileApplicationId.IsEmpty
            || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(normalizedKey)
            ? Result.Failure<MobilePage>(
                CmsErrors.Validation(
                    "MobilePage.Invalid",
                    "MobileApplicationId, Title, and PageKey are required."
                )
            )
            : Result.Success(
                new MobilePage(MobilePageId.Create())
                {
                    MobileApplicationId = mobileApplicationId,
                    MobileLayoutId = mobileLayoutId,
                    Title = title.Trim(),
                    PageKey = normalizedKey,
                    Route = normalizedRoute,
                    IconName = string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
                    Order = order,
                    ShowInNavigation = showInNavigation,
                    RequiresAuthentication = requiresAuthentication,
                    ContentChannelId = contentChannelId,
                    AdaptiveMessageKey = string.IsNullOrWhiteSpace(adaptiveMessageKey)
                        ? null
                        : CmsTextNormalizer.NormalizeKey(adaptiveMessageKey),
                    BlocksJson = string.IsNullOrWhiteSpace(blocksJson) ? "[]" : blocksJson.Trim(),
                    IsActive = true,
                }
            );
    }
}
