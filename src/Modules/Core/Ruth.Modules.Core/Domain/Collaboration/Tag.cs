using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Personal (user-owned) or organizational tag aggregate for lightweight entity categorization.</summary>
public sealed class Tag : AggregateRoot<TagId>, IAuditableEntity, IHasConcurrencyToken, ISecured
{
    /// <summary>Canonical entity type key for Tag.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Tag";

    private static readonly char[] _forbiddenChars = ['<', '>', '%', '&'];

    private readonly List<TaggedItem> _taggedItems = [];

    private Tag() { }

    private Tag(
        TagId id,
        Guid systemGuid,
        EntityTypeDescriptorId? entityTypeId,
        string name,
        string? description,
        CategoryId? categoryId,
        Guid? ownerUserId,
        string? iconName,
        string backgroundColor,
        int order,
        bool isSystem
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        EntityTypeId = entityTypeId;
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        Description = description;
        CategoryId = categoryId;
        OwnerUserId = ownerUserId;
        IconName = iconName;
        BackgroundColor = backgroundColor;
        Order = Math.Max(0, order);
        IsActive = true;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the optional entity type descriptor identifier this tag applies to.</summary>
    public EntityTypeDescriptorId? EntityTypeId { get; private set; }

    /// <summary>Gets the tag display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the normalized uppercase tag name for scope uniqueness checks.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional tag description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional category identifier.</summary>
    public CategoryId? CategoryId { get; private set; }

    /// <summary>Gets the owner user identifier for personal tags, or null for organizational tags.</summary>
    public Guid? OwnerUserId { get; private set; }

    /// <summary>Gets whether this tag is a personal tag owned by a single user.</summary>
    public bool IsPersonal => OwnerUserId.HasValue;

    /// <summary>Gets the optional icon name.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the pill badge background color.</summary>
    public string BackgroundColor { get; private set; } = "#F4F5F7";

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this tag is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether this tag is system-protected.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets tagged item associations.</summary>
    public IReadOnlyCollection<TaggedItem> TaggedItems => _taggedItems.AsReadOnly();

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        CategoryId.HasValue
            ? new SecuredEntityRef(
                Category.EntityTypeKeyName,
                CategoryId.Value.Value,
                GlobalDefaultSecured.Instance
            )
            : GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Checks whether <paramref name="viewerUserId" /> can access this tag (personal tags are owner-only).</summary>
    public bool IsAccessibleBy(Guid? viewerUserId)
    {
        return !IsPersonal || (viewerUserId.HasValue && OwnerUserId!.Value == viewerUserId.Value);
    }

    /// <summary>Creates a validated personal or organizational tag.</summary>
    public static Result<Tag> Create(
        string name,
        EntityTypeDescriptorId? entityTypeId = null,
        string? description = null,
        CategoryId? categoryId = null,
        Guid? ownerUserId = null,
        string? iconName = null,
        string? backgroundColor = "#F4F5F7",
        int order = 0,
        bool isSystem = false
    )
    {
        if (
            string.IsNullOrWhiteSpace(name)
            || name.Trim().Length > CoreTextPolicy.NameMaxLength
            || name.IndexOfAny(_forbiddenChars) >= 0
        )
        {
            return CoreCollaborationErrors.TagNameInvalid;
        }

        var id = TagId.Create();
        return new Tag(
            id,
            id.Value,
            entityTypeId,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            categoryId,
            ownerUserId,
            string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
            string.IsNullOrWhiteSpace(backgroundColor) ? "#F4F5F7" : backgroundColor.Trim(),
            order,
            isSystem
        );
    }
}
