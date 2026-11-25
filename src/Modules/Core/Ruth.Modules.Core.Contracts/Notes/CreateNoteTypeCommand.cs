using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Creates a new note type for an entity type.</summary>
public sealed record CreateNoteTypeCommand(
    Guid EntityTypeId,
    string Name,
    bool UserSelectable = true,
    string? IconName = null,
    string? Color = null,
    int Order = 0,
    bool RequiresApprovals = false,
    bool AllowsWatching = true,
    bool AllowsReplies = true,
    int? MaxReplyDepth = 3,
    bool AutoWatchAuthors = false,
    bool AllowsAttachments = false,
    Guid? BinaryFileTypeId = null,
    bool IsMentionEnabled = true
) : ICommand<NoteTypeResponse>;
