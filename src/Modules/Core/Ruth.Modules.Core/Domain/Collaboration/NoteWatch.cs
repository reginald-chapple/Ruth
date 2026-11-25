using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Subscription allowing users to watch notes, note types, or entities with optional non-overridable lock.</summary>
public sealed class NoteWatch : AggregateRoot<NoteWatchId>, IAuditableEntity
{
    private NoteWatch() { }

    private NoteWatch(
        NoteWatchId id,
        Guid watcherUserId,
        bool isWatching,
        bool watchReplies,
        bool allowOverride,
        NoteTypeId? noteTypeId,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        NoteId? noteId
    )
        : base(id)
    {
        WatcherUserId = watcherUserId;
        IsWatching = isWatching;
        WatchReplies = watchReplies;
        AllowOverride = allowOverride;
        NoteTypeId = noteTypeId;
        EntityTypeId = entityTypeId;
        EntityId = entityId;
        NoteId = noteId;
    }

    /// <summary>Gets the watching user identifier.</summary>
    public Guid WatcherUserId { get; private set; }

    /// <summary>Gets whether the user is actively watching (false represents an explicit unwatch override).</summary>
    public bool IsWatching { get; private set; } = true;

    /// <summary>Gets whether replies also trigger watch notifications.</summary>
    public bool WatchReplies { get; private set; }

    /// <summary>Gets whether the user is allowed to override/unwatch this subscription.</summary>
    public bool AllowOverride { get; private set; } = true;

    /// <summary>Gets the optional watched note type identifier.</summary>
    public NoteTypeId? NoteTypeId { get; private set; }

    /// <summary>Gets the optional watched entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId? EntityTypeId { get; private set; }

    /// <summary>Gets the optional watched target entity identifier.</summary>
    public Guid? EntityId { get; private set; }

    /// <summary>Gets the optional watched note identifier.</summary>
    public NoteId? NoteId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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

    /// <summary>Returns whether this watch covers the candidate unwatch scope.</summary>
    public bool CoversScope(
        NoteTypeId? noteTypeId,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        NoteId? noteId
    )
    {
        return NoteId.HasValue ? noteId.HasValue && NoteId.Value == noteId.Value
            : EntityId.HasValue
                ? entityId.HasValue
                    && EntityId.Value == entityId.Value
                    && (!NoteTypeId.HasValue || NoteTypeId == noteTypeId)
            : NoteTypeId.HasValue ? noteTypeId.HasValue && NoteTypeId.Value == noteTypeId.Value
            : EntityTypeId.HasValue
                && entityTypeId.HasValue
                && EntityTypeId.Value == entityTypeId.Value;
    }

    /// <summary>Updates watch state.</summary>
    public void Update(bool isWatching, bool watchReplies, bool allowOverride)
    {
        IsWatching = isWatching;
        WatchReplies = watchReplies;
        AllowOverride = allowOverride;
    }

    /// <summary>Creates a validated note watch subscription.</summary>
    public static Result<NoteWatch> Create(
        Guid watcherUserId,
        bool isWatching,
        bool watchReplies,
        bool allowOverride,
        NoteTypeId? noteTypeId,
        EntityTypeDescriptorId? entityTypeId,
        Guid? entityId,
        NoteId? noteId
    )
    {
        return !noteTypeId.HasValue && !entityTypeId.HasValue && !noteId.HasValue
            ? (Result<NoteWatch>)CoreCollaborationErrors.NoteWatchFilterRequired
            : (Result<NoteWatch>)
                new NoteWatch(
                    NoteWatchId.Create(),
                    watcherUserId,
                    isWatching,
                    watchReplies,
                    allowOverride,
                    noteTypeId,
                    entityTypeId,
                    entityId,
                    noteId
                );
    }
}
