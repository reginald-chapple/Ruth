using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>External website, member portal, mobile, or display site aggregate with multi-host routing.</summary>
public sealed class CmsSite
    : AggregateRoot<CmsSiteId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Cms.Site";

    private readonly List<SiteDomain> _domains = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private CmsSite() { }

    private CmsSite(CmsSiteId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CmsThemeId? ThemeId { get; private set; }
    public CmsPageId? DefaultPageId { get; private set; }
    public CmsPageId? LoginPageId { get; private set; }
    public CmsPageId? NotFoundPageId { get; private set; }
    public Guid? BranchId { get; private set; }
    public CmsSiteType SiteType { get; private set; }
    public bool EnablePageViews { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<SiteDomain> Domains => _domains.AsReadOnly();

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<CmsSite> Create(
        string name,
        string slug,
        string? description = null,
        CmsThemeId? themeId = null,
        Guid? branchId = null,
        CmsSiteType siteType = CmsSiteType.Web,
        bool enablePageViews = true
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<CmsSite>(
                CmsErrors.Validation("Site.NameRequired", "Site name is required.")
            );
        }

        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return Result.Failure<CmsSite>(
                CmsErrors.Validation("Site.SlugInvalid", "Site slug is invalid.")
            );
        }

        var site = new CmsSite(CmsSiteId.Create())
        {
            Name = name.Trim(),
            Slug = normalizedSlug,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ThemeId = themeId,
            BranchId = branchId,
            SiteType = siteType,
            EnablePageViews = enablePageViews,
            IsActive = true,
        };

        return Result.Success(site);
    }

    public Result<SiteDomain> AddDomain(string domain, bool isPrimary = false)
    {
        var domainRes = SiteDomain.Create(
            Id,
            domain,
            _domains.Count,
            isPrimary || _domains.Count == 0
        );
        if (domainRes.IsFailure)
        {
            return domainRes;
        }

        if (
            _domains.Exists(d =>
                string.Equals(d.Domain, domainRes.Value.Domain, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Result.Failure<SiteDomain>(
                CmsErrors.Conflict(
                    "SiteDomain.Duplicate",
                    $"Domain '{domainRes.Value.Domain}' is already assigned to site '{Slug}'."
                )
            );
        }

        if (domainRes.Value.IsPrimary)
        {
            foreach (var existing in _domains)
            {
                existing.SetPrimary(isPrimary: false);
            }
        }

        _domains.Add(domainRes.Value);
        return domainRes;
    }

    public void SetDefaultPage(CmsPageId pageId)
    {
        DefaultPageId = pageId;
    }

    public void SetSystemPages(CmsPageId? loginPageId, CmsPageId? notFoundPageId)
    {
        LoginPageId = loginPageId;
        NotFoundPageId = notFoundPageId;
    }
}
