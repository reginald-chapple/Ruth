using Ruth.Modules.Core.Domain.Files;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Configuration aggregate governing notes, approvals, threading depth, watching, and attachments.</summary>
public sealed class NoteType
    : AggregateRoot<NoteTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for NoteType.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.NoteType";

    private NoteType() { }

    private NoteType(
        NoteTypeId id,
        Guid systemGuid,
        EntityTypeDescriptorId entityTypeId,
        string name,
        bool userSelectable,
        string? iconName,
        string? color,
        int order,
        bool requiresApprovals,
        bool allowsWatching,
        bool allowsReplies,
        int? maxReplyDepth,
        bool autoWatchAuthors,
        bool allowsAttachments,
        BinaryFileTypeId? binaryFileTypeId,
        bool isMentionEnabled,
        bool isSystem
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        EntityTypeId = entityTypeId;
        Name = name;
        UserSelectable = userSelectable;
        IconName = iconName;
        Color = color;
        Order = Math.Max(0, order);
        RequiresApprovals = requiresApprovals;
        AllowsWatching = allowsWatching;
        AllowsReplies = allowsReplies;
        MaxReplyDepth = allowsReplies && maxReplyDepth is > 0 ? maxReplyDepth : null;
        AutoWatchAuthors = allowsWatching && autoWatchAuthors;
        AllowsAttachments = allowsAttachments;
        BinaryFileTypeId = allowsAttachments ? binaryFileTypeId : null;
        IsMentionEnabled = isMentionEnabled;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the entity type descriptor identifier this note type belongs to.</summary>
    public EntityTypeDescriptorId EntityTypeId { get; private set; }

    /// <summary>Gets the note type display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets whether staff may manually select this note type.</summary>
    public bool UserSelectable { get; private set; }

    /// <summary>Gets the optional icon name.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the optional accent color.</summary>
    public string? Color { get; private set; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether new or edited notes require moderation approval.</summary>
    public bool RequiresApprovals { get; private set; }

    /// <summary>Gets whether users may watch notes of this type.</summary>
    public bool AllowsWatching { get; private set; }

    /// <summary>Gets whether threaded replies are allowed.</summary>
    public bool AllowsReplies { get; private set; }

    /// <summary>Gets the optional maximum reply nesting depth.</summary>
    public int? MaxReplyDepth { get; private set; }

    /// <summary>Gets whether note authors automatically watch their own notes.</summary>
    public bool AutoWatchAuthors { get; private set; }

    /// <summary>Gets whether binary file attachments are allowed.</summary>
    public bool AllowsAttachments { get; private set; }

    /// <summary>Gets the backing binary file type identifier when attachments are enabled.</summary>
    public BinaryFileTypeId? BinaryFileTypeId { get; private set; }

    /// <summary>Gets whether @mentions are enabled.</summary>
    public bool IsMentionEnabled { get; private set; }

    /// <summary>Gets whether this note type is built-in.</summary>
    public bool IsSystem { get; private set; }

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
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated note type.</summary>
    public static Result<NoteType> Create(
        EntityTypeDescriptorId entityTypeId,
        string name,
        bool userSelectable = true,
        string? iconName = null,
        string? color = null,
        int order = 0,
        bool requiresApprovals = false,
        bool allowsWatching = true,
        bool allowsReplies = true,
        int? maxReplyDepth = 3,
        bool autoWatchAuthors = false,
        bool allowsAttachments = false,
        BinaryFileTypeId? binaryFileTypeId = null,
        bool isMentionEnabled = true,
        bool isSystem = false
    )
    {
        if (entityTypeId == EntityTypeDescriptorId.Empty)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreCollaborationErrors.NoteTypeNameRequired,
            CoreCollaborationErrors.NoteTypeNameRequired
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<NoteType>(nameResult.Errors);
        }

        var id = NoteTypeId.Create();
        return new NoteType(
            id,
            id.Value,
            entityTypeId,
            nameResult.Value,
            userSelectable,
            string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
            string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
            order,
            requiresApprovals,
            allowsWatching,
            allowsReplies,
            maxReplyDepth,
            autoWatchAuthors,
            allowsAttachments,
            binaryFileTypeId,
            isMentionEnabled,
            isSystem
        );
    }
}
