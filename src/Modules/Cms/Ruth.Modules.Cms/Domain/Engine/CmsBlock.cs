using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Configured block instance placed inside a Page, Layout, or Site zone.</summary>
public sealed class CmsBlock
    : AggregateRoot<CmsBlockId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Cms.Block";

    private Dictionary<string, string?> _settings = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private CmsBlock() { }

    private CmsBlock(CmsBlockId id)
        : base(id) { }

    public CmsBlockTypeId BlockTypeId { get; private set; }
    public CmsPageId? PageId { get; private set; }
    public CmsLayoutId? LayoutId { get; private set; }
    public CmsSiteId? SiteId { get; private set; }
    public string Zone { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public string? PreHtml { get; private set; }
    public string? PostHtml { get; private set; }
    public string? CssClass { get; private set; }
    public int OutputCacheDurationSeconds { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyDictionary<string, string?> Settings
    {
        get => _settings;
        private set =>
            _settings = new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

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

    public static Result<CmsBlock> Create(
        CmsBlockTypeId blockTypeId,
        string zone,
        string name,
        CmsPageId? pageId = null,
        CmsLayoutId? layoutId = null,
        CmsSiteId? siteId = null,
        int order = 0,
        string? preHtml = null,
        string? postHtml = null,
        string? cssClass = null,
        int outputCacheDurationSeconds = 0,
        IReadOnlyDictionary<string, string?>? settings = null
    )
    {
        var scopeCount =
            (pageId is not null ? 1 : 0)
            + (layoutId is not null ? 1 : 0)
            + (siteId is not null ? 1 : 0);

        if (
            blockTypeId.IsEmpty
            || string.IsNullOrWhiteSpace(zone)
            || string.IsNullOrWhiteSpace(name)
            || scopeCount != 1
        )
        {
            return Result.Failure<CmsBlock>(
                CmsErrors.Validation(
                    "Block.Invalid",
                    "Block requires a BlockTypeId, Zone, Name, and exactly one owner scope (Page, Layout, or Site)."
                )
            );
        }

        if (outputCacheDurationSeconds < 0)
        {
            return Result.Failure<CmsBlock>(
                CmsErrors.Validation(
                    "Block.CacheDurationInvalid",
                    "Output cache duration cannot be negative."
                )
            );
        }

        var mergedSettings = settings is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(settings, StringComparer.OrdinalIgnoreCase);

        return Result.Success(
            new CmsBlock(CmsBlockId.Create())
            {
                BlockTypeId = blockTypeId,
                PageId = pageId,
                LayoutId = layoutId,
                SiteId = siteId,
                Zone = zone.Trim(),
                Name = name.Trim(),
                Order = order,
                PreHtml = preHtml,
                PostHtml = postHtml,
                CssClass = string.IsNullOrWhiteSpace(cssClass) ? null : cssClass.Trim(),
                OutputCacheDurationSeconds = outputCacheDurationSeconds,
                IsActive = true,
                _settings = mergedSettings,
            }
        );
    }

    public Result UpdateConfiguration(
        string zone,
        string name,
        int order,
        bool isActive,
        string? preHtml,
        string? postHtml,
        string? cssClass,
        IReadOnlyDictionary<string, string?>? settings
    )
    {
        if (string.IsNullOrWhiteSpace(zone) || string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(
                CmsErrors.Validation("Block.Invalid", "Zone and Name are required.")
            );
        }

        Zone = zone.Trim();
        Name = name.Trim();
        Order = order;
        IsActive = isActive;
        PreHtml = preHtml;
        PostHtml = postHtml;
        CssClass = string.IsNullOrWhiteSpace(cssClass) ? null : cssClass.Trim();
        if (settings is not null)
        {
            _settings = new Dictionary<string, string?>(settings, StringComparer.OrdinalIgnoreCase);
        }

        return Result.Success();
    }
}
