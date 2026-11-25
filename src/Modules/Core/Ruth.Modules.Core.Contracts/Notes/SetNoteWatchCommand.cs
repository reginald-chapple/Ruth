using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Creates or updates a note watch subscription enforcing non-overridable watch rules.</summary>
public sealed record SetNoteWatchCommand(
    Guid WatcherUserId,
    bool IsWatching,
    bool WatchReplies = true,
    bool AllowOverride = true,
    Guid? NoteTypeId = null,
    Guid? EntityTypeId = null,
    Guid? EntityId = null,
    Guid? NoteId = null
) : ICommand<NoteWatchResponse>;
