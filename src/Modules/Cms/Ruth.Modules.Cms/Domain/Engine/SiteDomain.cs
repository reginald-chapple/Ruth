using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Host domain mapping attached to a CMS <see cref="CmsSite" /> for multi-host routing.</summary>
public sealed class SiteDomain : Entity<SiteDomainId>
{
    private SiteDomain() { }

    private SiteDomain(SiteDomainId id, CmsSiteId siteId, string domain, int order, bool isPrimary)
        : base(id)
    {
        SiteId = siteId;
        Domain = domain;
        Order = order;
        IsPrimary = isPrimary;
    }

    public CmsSiteId SiteId { get; private set; }
    public string Domain { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public bool IsPrimary { get; private set; }

    public static Result<SiteDomain> Create(
        CmsSiteId siteId,
        string domain,
        int order = 0,
        bool isPrimary = false
    )
    {
        var normalized = CmsTextNormalizer.NormalizeHost(domain);
        return string.IsNullOrWhiteSpace(normalized) || normalized.Length > 253
            ? Result.Failure<SiteDomain>(
                CmsErrors.Validation("SiteDomain.Invalid", "Site domain must be a valid host name.")
            )
            : Result.Success(
                new SiteDomain(SiteDomainId.Create(), siteId, normalized, order, isPrimary)
            );
    }

    internal void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}
