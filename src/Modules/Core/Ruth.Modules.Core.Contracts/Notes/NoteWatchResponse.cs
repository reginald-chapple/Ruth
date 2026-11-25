namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Represents a user watch subscription on a note, note type, or entity.</summary>
public sealed record NoteWatchResponse(
    Guid Id,
    Guid? NoteTypeId,
    Guid? EntityTypeId,
    Guid? EntityId,
    Guid? NoteId,
    Guid WatcherUserId,
    bool IsWatching,
    bool WatchReplies,
    bool AllowOverride,
    DateTimeOffset CreatedOnUtc
);
