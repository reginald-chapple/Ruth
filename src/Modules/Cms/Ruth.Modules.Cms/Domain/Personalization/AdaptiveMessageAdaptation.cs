using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Personalization;

/// <summary>Prioritized segment-targeted content variant belonging to an <see cref="AdaptiveMessage" />.</summary>
public sealed class AdaptiveMessageAdaptation : Entity<AdaptiveMessageAdaptationId>
{
    private List<string> _segmentKeys = [];

    private AdaptiveMessageAdaptation() { }

    private AdaptiveMessageAdaptation(AdaptiveMessageAdaptationId id)
        : base(id) { }

    public AdaptiveMessageId AdaptiveMessageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public DateTimeOffset? StartDateTimeUtc { get; private set; }
    public DateTimeOffset? ExpireDateTimeUtc { get; private set; }
    public int? ViewSaturationCount { get; private set; }
    public int? ViewSaturationInDays { get; private set; }
    public string Headline { get; private set; } = string.Empty;
    public string BodyMarkup { get; private set; } = string.Empty;
    public string? CallToActionText { get; private set; }
    public string? CallToActionUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<string> SegmentKeys
    {
        get => _segmentKeys;
        private set => _segmentKeys = [.. value];
    }

    public static Result<AdaptiveMessageAdaptation> Create(
        AdaptiveMessageId adaptiveMessageId,
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
        var error = ValidateCreate(
            adaptiveMessageId,
            name,
            headline,
            startDateTimeUtc,
            expireDateTimeUtc,
            viewSaturationCount,
            viewSaturationInDays
        );
        if (error is not null)
        {
            return Result.Failure<AdaptiveMessageAdaptation>(error);
        }

        List<string> normalizedSegments = segmentKeys is null
            ? []
            :
            [
                .. segmentKeys
                    .Select(CmsTextNormalizer.NormalizeKey)
                    .Where(static k => k.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ];

        return Result.Success(
            new AdaptiveMessageAdaptation(AdaptiveMessageAdaptationId.Create())
            {
                AdaptiveMessageId = adaptiveMessageId,
                Name = name.Trim(),
                Headline = headline.Trim(),
                BodyMarkup = bodyMarkup ?? string.Empty,
                Order = order,
                _segmentKeys = normalizedSegments,
                StartDateTimeUtc = startDateTimeUtc?.ToUniversalTime(),
                ExpireDateTimeUtc = expireDateTimeUtc?.ToUniversalTime(),
                ViewSaturationCount = viewSaturationCount,
                ViewSaturationInDays = viewSaturationInDays,
                CallToActionText = TrimOrNull(callToActionText),
                CallToActionUrl = TrimOrNull(callToActionUrl),
                ImageUrl = TrimOrNull(imageUrl),
                IsActive = true,
            }
        );
    }

    private static DomainError? ValidateCreate(
        AdaptiveMessageId adaptiveMessageId,
        string name,
        string headline,
        DateTimeOffset? startDateTimeUtc,
        DateTimeOffset? expireDateTimeUtc,
        int? viewSaturationCount,
        int? viewSaturationInDays
    )
    {
        return adaptiveMessageId.IsEmpty
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(headline)
                ? CmsErrors.Validation(
                    "AdaptiveMessageAdaptation.Invalid",
                    "AdaptiveMessageId, Name, and Headline are required."
                )
            : startDateTimeUtc is not null
            && expireDateTimeUtc is not null
            && expireDateTimeUtc <= startDateTimeUtc
                ? CmsErrors.Validation(
                    "AdaptiveMessageAdaptation.WindowInvalid",
                    "ExpireDateTimeUtc must be after StartDateTimeUtc."
                )
            : viewSaturationCount is <= 0 || viewSaturationInDays is <= 0
                ? CmsErrors.Validation(
                    "AdaptiveMessageAdaptation.SaturationInvalid",
                    "View saturation count and lookback days must be positive."
                )
            : null;
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool IsWindowActiveAt(DateTimeOffset nowUtc)
    {
        return IsActive
            && (StartDateTimeUtc is null || !(StartDateTimeUtc > nowUtc))
            && (ExpireDateTimeUtc is null || ExpireDateTimeUtc > nowUtc);
    }
}
