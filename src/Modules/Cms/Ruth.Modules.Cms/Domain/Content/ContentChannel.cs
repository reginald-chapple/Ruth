using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Structured content publication channel (news, articles, announcements, stories, resources).</summary>
public sealed class ContentChannel
    : AggregateRoot<ContentChannelId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Cms.ContentChannel";

    private List<Guid> _childChannelIds = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private ContentChannel() { }

    private ContentChannel(ContentChannelId id)
        : base(id) { }

    public ContentChannelTypeId ContentChannelTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconCssClass { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool EnableRss { get; private set; } = true;
    public string? ChannelUrl { get; private set; }
    public string? ItemUrl { get; private set; }
    public int TimeToLiveMinutes { get; private set; } = 60;
    public bool ItemsManuallyOrdered { get; private set; }
    public bool IsStructuredContent { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyList<Guid> ChildChannelIds
    {
        get => _childChannelIds;
        private set => _childChannelIds = [.. value];
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

    public static Result<ContentChannel> Create(
        ContentChannelTypeId contentChannelTypeId,
        string name,
        string slug,
        string? description = null,
        string? iconCssClass = null,
        bool requiresApproval = false,
        bool enableRss = true,
        string? channelUrl = null,
        string? itemUrl = null,
        int timeToLiveMinutes = 60,
        bool itemsManuallyOrdered = false,
        bool isStructuredContent = false
    )
    {
        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        return
            contentChannelTypeId.IsEmpty
            || string.IsNullOrWhiteSpace(name)
            || string.IsNullOrWhiteSpace(normalizedSlug)
            ? Result.Failure<ContentChannel>(
                CmsErrors.Validation(
                    "ContentChannel.Invalid",
                    "ContentChannelTypeId, Name, and Slug are required."
                )
            )
            : Result.Success(
                new ContentChannel(ContentChannelId.Create())
                {
                    ContentChannelTypeId = contentChannelTypeId,
                    Name = name.Trim(),
                    Slug = normalizedSlug,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    IconCssClass = string.IsNullOrWhiteSpace(iconCssClass)
                        ? null
                        : iconCssClass.Trim(),
                    RequiresApproval = requiresApproval,
                    EnableRss = enableRss,
                    ChannelUrl = string.IsNullOrWhiteSpace(channelUrl) ? null : channelUrl.Trim(),
                    ItemUrl = string.IsNullOrWhiteSpace(itemUrl) ? null : itemUrl.Trim(),
                    TimeToLiveMinutes = Math.Clamp(timeToLiveMinutes, 1, 1440),
                    ItemsManuallyOrdered = itemsManuallyOrdered,
                    IsStructuredContent = isStructuredContent,
                    IsActive = true,
                }
            );
    }

    public void AddChildChannel(ContentChannelId childId)
    {
        if (!childId.IsEmpty && childId != Id && !_childChannelIds.Contains(childId.Value))
        {
            _childChannelIds.Add(childId.Value);
        }
    }
}
