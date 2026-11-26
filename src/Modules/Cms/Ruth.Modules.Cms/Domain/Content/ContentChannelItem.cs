using Ruth.Modules.Cms.Contracts.Content;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Publishable item inside a <see cref="ContentChannel" /> with scheduling, approval, slug, topics, and associations.</summary>
public sealed class ContentChannelItem
    : AggregateRoot<ContentChannelItemId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Cms.ContentChannelItem";

    private readonly List<ContentChannelItemAssociation> _childAssociations = [];
    private List<Guid> _topicIds = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private ContentChannelItem() { }

    private ContentChannelItem(ContentChannelItemId id)
        : base(id) { }

    public ContentChannelId ContentChannelId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public ContentChannelItemStatus Status { get; private set; } =
        ContentChannelItemStatus.Approved;
    public Guid? ApprovedByPersonId { get; private set; }
    public DateTimeOffset? ApprovedDateTimeUtc { get; private set; }
    public DateTimeOffset StartDateTimeUtc { get; private set; }
    public DateTimeOffset? ExpireDateTimeUtc { get; private set; }
    public int Order { get; private set; }
    public Guid? AuthorPersonId { get; private set; }
    public ContentTopicId? PrimaryTopicId { get; private set; }

    public IReadOnlyCollection<ContentChannelItemAssociation> ChildAssociations =>
        _childAssociations.AsReadOnly();

    public IReadOnlyList<Guid> TopicIds
    {
        get => _topicIds;
        private set => _topicIds = [.. value];
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

    public static Result<ContentChannelItem> Create(
        ContentChannelId contentChannelId,
        string title,
        string slug,
        string content,
        bool requiresApproval,
        DateTimeOffset startDateTimeUtc,
        DateTimeOffset? expireDateTimeUtc = null,
        string? summary = null,
        int priority = 0,
        int order = 0,
        Guid? authorPersonId = null,
        ContentTopicId? primaryTopicId = null,
        IReadOnlyList<Guid>? topicIds = null,
        IReadOnlyDictionary<string, string?>? attributeValues = null,
        DateTimeOffset? nowUtc = null
    )
    {
        var validation = ValidateCreate(
            contentChannelId,
            title,
            slug,
            startDateTimeUtc,
            expireDateTimeUtc,
            out var normalizedSlug
        );
        if (validation is not null)
        {
            return Result.Failure<ContentChannelItem>(validation);
        }

        var normalizedTopics = NormalizeTopics(primaryTopicId, topicIds);
        var status = requiresApproval
            ? ContentChannelItemStatus.PendingApproval
            : ContentChannelItemStatus.Approved;
        var approved = status == ContentChannelItemStatus.Approved;

        var item = new ContentChannelItem(ContentChannelItemId.Create())
        {
            ContentChannelId = contentChannelId,
            Title = title.Trim(),
            Slug = normalizedSlug,
            Summary = TrimOrNull(summary),
            Content = content ?? string.Empty,
            Priority = priority,
            Status = status,
            ApprovedByPersonId = approved ? authorPersonId : null,
            ApprovedDateTimeUtc = approved ? (nowUtc ?? startDateTimeUtc).ToUniversalTime() : null,
            StartDateTimeUtc = startDateTimeUtc.ToUniversalTime(),
            ExpireDateTimeUtc = expireDateTimeUtc?.ToUniversalTime(),
            Order = order,
            AuthorPersonId = authorPersonId,
            PrimaryTopicId = primaryTopicId,
            _topicIds = normalizedTopics,
            _attributeValues = attributeValues is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(
                    attributeValues,
                    StringComparer.OrdinalIgnoreCase
                ),
        };

        return Result.Success(item);
    }

    private static DomainError? ValidateCreate(
        ContentChannelId contentChannelId,
        string title,
        string slug,
        DateTimeOffset startDateTimeUtc,
        DateTimeOffset? expireDateTimeUtc,
        out string normalizedSlug
    )
    {
        normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        return contentChannelId.IsEmpty
            || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(normalizedSlug)
                ? CmsErrors.Validation(
                    "ContentChannelItem.Invalid",
                    "ContentChannelId, Title, and Slug are required."
                )
            : expireDateTimeUtc is not null && expireDateTimeUtc <= startDateTimeUtc
                ? CmsErrors.Validation(
                    "ContentChannelItem.WindowInvalid",
                    "ExpireDateTimeUtc must be after StartDateTimeUtc."
                )
            : null;
    }

    private static List<Guid> NormalizeTopics(
        ContentTopicId? primaryTopicId,
        IReadOnlyList<Guid>? topicIds
    )
    {
        List<Guid> normalized = topicIds is null
            ? []
            : [.. topicIds.Where(static g => g != Guid.Empty).Distinct()];
        if (
            primaryTopicId is { IsEmpty: false }
            && !normalized.Contains(primaryTopicId.Value.Value)
        )
        {
            normalized.Insert(0, primaryTopicId.Value.Value);
        }

        return normalized;
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public void Moderate(
        ContentChannelItemStatus status,
        Guid? approvedByPersonId,
        DateTimeOffset nowUtc
    )
    {
        Status = status;
        if (status == ContentChannelItemStatus.Approved)
        {
            ApprovedByPersonId = approvedByPersonId;
            ApprovedDateTimeUtc = nowUtc.ToUniversalTime();
        }
        else
        {
            ApprovedByPersonId = null;
            ApprovedDateTimeUtc = null;
        }
    }

    public Result AddAssociation(ContentChannelItemId childItemId, int order = 0)
    {
        if (childItemId.IsEmpty || childItemId == Id)
        {
            return Result.Failure(
                CmsErrors.Validation(
                    "ContentChannelItem.AssociationInvalid",
                    "Cannot associate an item with itself or an empty ID."
                )
            );
        }

        if (_childAssociations.Exists(a => a.ChildItemId == childItemId))
        {
            return Result.Success();
        }

        _childAssociations.Add(
            new ContentChannelItemAssociation(
                ContentChannelItemAssociationId.Create(),
                Id,
                childItemId,
                order
            )
        );
        return Result.Success();
    }

    public bool IsPublishedAt(DateTimeOffset nowUtc)
    {
        return Status == ContentChannelItemStatus.Approved
            && StartDateTimeUtc <= nowUtc
            && (ExpireDateTimeUtc is null || ExpireDateTimeUtc > nowUtc);
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (AuthorPersonId == retiredPersonId)
        {
            AuthorPersonId = survivingPersonId;
            changed = true;
        }

        if (ApprovedByPersonId == retiredPersonId)
        {
            ApprovedByPersonId = survivingPersonId;
            changed = true;
        }

        return changed;
    }
}
