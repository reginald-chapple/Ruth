using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Channels;

/// <summary>Digital Signage, Apple TV, or Roku feed application backend aggregate.</summary>
public sealed class SignageApplication
    : AggregateRoot<SignageApplicationId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Cms.SignageApplication";

    private List<SignageSlideSpec> _slides = [];

    private SignageApplication() { }

    private SignageApplication(SignageApplicationId id)
        : base(id) { }

    public CmsSiteId? SiteId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public SignagePlatformType Platform { get; private set; }
    public int RefreshIntervalSeconds { get; private set; } = 60;
    public CmsThemeId? ThemeId { get; private set; }
    public ContentChannelId? DefaultContentChannelId { get; private set; }
    public MediaFolderId? MediaFolderId { get; private set; }
    public string? AdaptiveMessageKey { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<SignageSlideSpec> Slides
    {
        get => _slides;
        private set => _slides = [.. value];
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

    public static Result<SignageApplication> Create(
        string name,
        string slug,
        SignagePlatformType platform = SignagePlatformType.DigitalSignage,
        CmsSiteId? siteId = null,
        int refreshIntervalSeconds = 60,
        CmsThemeId? themeId = null,
        ContentChannelId? defaultContentChannelId = null,
        MediaFolderId? mediaFolderId = null,
        string? adaptiveMessageKey = null,
        IReadOnlyList<SignageSlideSpec>? slides = null
    )
    {
        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Failure<SignageApplication>(
                CmsErrors.Validation(
                    "SignageApplication.Invalid",
                    "Signage application name and slug are required."
                )
            );
        }

        List<SignageSlideSpec> normalizedSlides = slides is null
            ? []
            : [.. slides.OrderBy(static s => s.Order)];

        return Result.Success(
            new SignageApplication(SignageApplicationId.Create())
            {
                SiteId = siteId,
                Name = name.Trim(),
                Slug = normalizedSlug,
                Platform = platform,
                RefreshIntervalSeconds = Math.Clamp(refreshIntervalSeconds, 5, 3600),
                ThemeId = themeId,
                DefaultContentChannelId = defaultContentChannelId,
                MediaFolderId = mediaFolderId,
                AdaptiveMessageKey = string.IsNullOrWhiteSpace(adaptiveMessageKey)
                    ? null
                    : CmsTextNormalizer.NormalizeKey(adaptiveMessageKey),
                _slides = normalizedSlides,
                IsActive = true,
            }
        );
    }

    public IReadOnlyList<SignageSlideSpec> GetActiveSlidesAt(DateTimeOffset nowUtc)
    {
        return
        [
            .. _slides
                .Where(s =>
                    (s.StartDateTimeUtc is null || s.StartDateTimeUtc <= nowUtc)
                    && (s.ExpireDateTimeUtc is null || s.ExpireDateTimeUtc > nowUtc)
                )
                .OrderBy(static s => s.Order),
        ];
    }
}
