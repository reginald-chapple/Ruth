using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>CMS page aggregate bound to a Site and Layout with friendly routes and entity contexts.</summary>
public sealed class CmsPage
    : AggregateRoot<CmsPageId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Cms.Page";

    private readonly List<PageRoute> _routes = [];
    private readonly List<PageContext> _contexts = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private CmsPage() { }

    private CmsPage(CmsPageId id)
        : base(id) { }

    public CmsSiteId SiteId { get; private set; }
    public CmsLayoutId LayoutId { get; private set; }
    public CmsPageId? ParentPageId { get; private set; }
    public string InternalName { get; private set; } = string.Empty;
    public string PageTitle { get; private set; } = string.Empty;
    public string BrowserTitle { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconCssClass { get; private set; }
    public int Order { get; private set; }
    public PageNavDisplayMode DisplayInNavWhen { get; private set; }
    public bool RequiresEncryption { get; private set; }
    public int OutputCacheDurationSeconds { get; private set; }
    public bool IsPublished { get; private set; } = true;

    public IReadOnlyCollection<PageRoute> Routes => _routes.AsReadOnly();
    public IReadOnlyCollection<PageContext> Contexts => _contexts.AsReadOnly();

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

    public static Result<CmsPage> Create(
        CmsSiteId siteId,
        CmsLayoutId layoutId,
        string internalName,
        string pageTitle,
        string? browserTitle = null,
        CmsPageId? parentPageId = null,
        string? description = null,
        string? iconCssClass = null,
        int order = 0,
        PageNavDisplayMode displayInNavWhen = PageNavDisplayMode.WhenAllowed,
        bool requiresEncryption = false,
        int outputCacheDurationSeconds = 0,
        bool isPublished = true
    )
    {
        return siteId.IsEmpty
            || layoutId.IsEmpty
            || string.IsNullOrWhiteSpace(internalName)
            || string.IsNullOrWhiteSpace(pageTitle)
                ? Result.Failure<CmsPage>(
                    CmsErrors.Validation(
                        "Page.Invalid",
                        "SiteId, LayoutId, InternalName, and PageTitle are required."
                    )
                )
            : outputCacheDurationSeconds < 0
                ? Result.Failure<CmsPage>(
                    CmsErrors.Validation(
                        "Page.CacheDurationInvalid",
                        "Output cache duration cannot be negative."
                    )
                )
            : Result.Success(
                new CmsPage(CmsPageId.Create())
                {
                    SiteId = siteId,
                    LayoutId = layoutId,
                    ParentPageId = parentPageId,
                    InternalName = internalName.Trim(),
                    PageTitle = pageTitle.Trim(),
                    BrowserTitle = string.IsNullOrWhiteSpace(browserTitle)
                        ? pageTitle.Trim()
                        : browserTitle.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    IconCssClass = string.IsNullOrWhiteSpace(iconCssClass)
                        ? null
                        : iconCssClass.Trim(),
                    Order = order,
                    DisplayInNavWhen = displayInNavWhen,
                    RequiresEncryption = requiresEncryption,
                    OutputCacheDurationSeconds = outputCacheDurationSeconds,
                    IsPublished = isPublished,
                }
            );
    }

    public Result<PageRoute> AddRoute(string route, bool isGlobal = false)
    {
        var routeRes = PageRoute.Create(Id, route, isGlobal);
        if (routeRes.IsFailure)
        {
            return routeRes;
        }

        if (
            _routes.Exists(r =>
                string.Equals(r.Route, routeRes.Value.Route, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Result.Failure<PageRoute>(
                CmsErrors.Conflict(
                    "PageRoute.Duplicate",
                    $"Route '{routeRes.Value.Route}' is already mapped to page '{InternalName}'."
                )
            );
        }

        _routes.Add(routeRes.Value);
        return routeRes;
    }

    public Result<PageContext> AddContext(string entityKey, string idParameter)
    {
        var ctxRes = PageContext.Create(Id, entityKey, idParameter);
        if (ctxRes.IsFailure)
        {
            return ctxRes;
        }

        if (
            _contexts.Exists(c =>
                string.Equals(
                    c.EntityKey,
                    ctxRes.Value.EntityKey,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            return Result.Failure<PageContext>(
                CmsErrors.Conflict(
                    "PageContext.Duplicate",
                    $"Context '{ctxRes.Value.EntityKey}' already exists on page '{InternalName}'."
                )
            );
        }

        _contexts.Add(ctxRes.Value);
        return ctxRes;
    }
}
