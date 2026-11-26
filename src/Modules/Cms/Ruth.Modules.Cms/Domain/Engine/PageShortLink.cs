using System.Text;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Shortened redirect URL with UTM parameters, click counting, and QR code support.</summary>
public sealed class PageShortLink
    : AggregateRoot<PageShortLinkId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private PageShortLink() { }

    private PageShortLink(PageShortLinkId id)
        : base(id) { }

    public CmsSiteId SiteId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public Guid? CategoryId { get; private set; }
    public bool IsPinned { get; private set; }
    public long ClickCount { get; private set; }
    public string? UtmSource { get; private set; }
    public string? UtmMedium { get; private set; }
    public string? UtmCampaign { get; private set; }
    public DateTimeOffset? LastClickedUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

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

    public static Result<PageShortLink> Create(
        CmsSiteId siteId,
        string token,
        string targetUrl,
        Guid? categoryId = null,
        bool isPinned = false,
        string? utmSource = null,
        string? utmMedium = null,
        string? utmCampaign = null
    )
    {
        var normalizedToken = CmsTextNormalizer.NormalizeSlug(token);
        return siteId.IsEmpty
            || string.IsNullOrWhiteSpace(normalizedToken)
            || normalizedToken.Length > 64
                ? Result.Failure<PageShortLink>(
                    CmsErrors.Validation(
                        "ShortLink.TokenInvalid",
                        "SiteId and a URL-safe token (1-64 chars) are required."
                    )
                )
            : !CmsTextNormalizer.IsSafeHttpOrRelativeUrl(targetUrl)
                ? Result.Failure<PageShortLink>(
                    CmsErrors.Validation(
                        "ShortLink.UrlInvalid",
                        "TargetUrl must be an http/https URL or root-relative path."
                    )
                )
            : Result.Success(
                new PageShortLink(PageShortLinkId.Create())
                {
                    SiteId = siteId,
                    Token = normalizedToken,
                    TargetUrl = targetUrl.Trim(),
                    CategoryId = categoryId,
                    IsPinned = isPinned,
                    UtmSource = string.IsNullOrWhiteSpace(utmSource) ? null : utmSource.Trim(),
                    UtmMedium = string.IsNullOrWhiteSpace(utmMedium) ? null : utmMedium.Trim(),
                    UtmCampaign = string.IsNullOrWhiteSpace(utmCampaign)
                        ? null
                        : utmCampaign.Trim(),
                }
            );
    }

    public string BuildResolvedTargetUrl()
    {
        List<string> utmPairs = [];
        if (!string.IsNullOrWhiteSpace(UtmSource))
        {
            utmPairs.Add($"utm_source={Uri.EscapeDataString(UtmSource)}");
        }

        if (!string.IsNullOrWhiteSpace(UtmMedium))
        {
            utmPairs.Add($"utm_medium={Uri.EscapeDataString(UtmMedium)}");
        }

        if (!string.IsNullOrWhiteSpace(UtmCampaign))
        {
            utmPairs.Add($"utm_campaign={Uri.EscapeDataString(UtmCampaign)}");
        }

        if (utmPairs.Count == 0)
        {
            return TargetUrl;
        }

        var separator = TargetUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var sb = new StringBuilder(TargetUrl);
        _ = sb.Append(separator).AppendJoin('&', utmPairs);
        return sb.ToString();
    }

    public void RecordClick(DateTimeOffset clickedAtUtc)
    {
        ClickCount++;
        LastClickedUtc = clickedAtUtc.ToUniversalTime();
    }
}
