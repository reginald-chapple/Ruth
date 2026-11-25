namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Configuration defining behavior, threading, attachments, and approvals for a note type.</summary>
public sealed record NoteTypeResponse(
    Guid Id,
    Guid SystemGuid,
    Guid EntityTypeId,
    string Name,
    bool UserSelectable,
    string? IconName,
    string? Color,
    int Order,
    bool RequiresApprovals,
    bool AllowsWatching,
    bool AllowsReplies,
    int? MaxReplyDepth,
    bool AutoWatchAuthors,
    bool AllowsAttachments,
    Guid? BinaryFileTypeId,
    bool IsMentionEnabled,
    bool IsSystem,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
