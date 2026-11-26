using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Targeted content campaign selecting the highest-priority eligible adaptation per viewer.</summary>
public sealed class AdaptiveMessage
    : AggregateRoot<AdaptiveMessageId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Cms.AdaptiveMessage";

    private readonly List<AdaptiveMessageAdaptation> _adaptations = [];

    private AdaptiveMessage() { }

    private AdaptiveMessage(AdaptiveMessageId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public DateTimeOffset? StartDateTimeUtc { get; private set; }
    public DateTimeOffset? ExpireDateTimeUtc { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<AdaptiveMessageAdaptation> Adaptations => _adaptations.AsReadOnly();

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

    public static Result<AdaptiveMessage> Create(
        string name,
        string key,
        string? description = null,
        Guid? categoryId = null,
        DateTimeOffset? startDateTimeUtc = null,
        DateTimeOffset? expireDateTimeUtc = null,
        bool isActive = true
    )
    {
        var normalizedKey = CmsTextNormalizer.NormalizeKey(key);
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedKey)
                ? Result.Failure<AdaptiveMessage>(
                    CmsErrors.Validation(
                        "AdaptiveMessage.Invalid",
                        "AdaptiveMessage name and key are required."
                    )
                )
            : startDateTimeUtc is not null
            && expireDateTimeUtc is not null
            && expireDateTimeUtc <= startDateTimeUtc
                ? Result.Failure<AdaptiveMessage>(
                    CmsErrors.Validation(
                        "AdaptiveMessage.WindowInvalid",
                        "ExpireDateTimeUtc must be after StartDateTimeUtc."
                    )
                )
            : Result.Success(
                new AdaptiveMessage(AdaptiveMessageId.Create())
                {
                    Name = name.Trim(),
                    Key = normalizedKey,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    CategoryId = categoryId,
                    StartDateTimeUtc = startDateTimeUtc?.ToUniversalTime(),
                    ExpireDateTimeUtc = expireDateTimeUtc?.ToUniversalTime(),
                    IsActive = isActive,
                }
            );
    }

    public Result<AdaptiveMessageAdaptation> AddAdaptation(
        string name,
        string headline,
        string bodyMarkup,
        int order = 0,
        IReadOnlyList<string>? segmentKeys = null,
        DateTimeOffset? startDateTimeUtc = null,
        DateTimeOffset? expireDateTimeUtc = null,
        int? viewSaturationCount = null,
        int? viewSaturationInDays = null,
        string? callToActionText = null,
        string? callToActionUrl = null,
        string? imageUrl = null
    )
    {
        var adaptationRes = AdaptiveMessageAdaptation.Create(
            Id,
            name,
            headline,
            bodyMarkup,
            order,
            segmentKeys,
            startDateTimeUtc,
            expireDateTimeUtc,
            viewSaturationCount,
            viewSaturationInDays,
            callToActionText,
            callToActionUrl,
            imageUrl
        );

        if (adaptationRes.IsFailure)
        {
            return adaptationRes;
        }

        _adaptations.Add(adaptationRes.Value);
        return adaptationRes;
    }

    public bool IsWindowActiveAt(DateTimeOffset nowUtc)
    {
        return IsActive
            && (StartDateTimeUtc is null || !(StartDateTimeUtc > nowUtc))
            && (ExpireDateTimeUtc is null || ExpireDateTimeUtc > nowUtc);
    }
}
