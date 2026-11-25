namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Threaded note item with attachments and nested replies.</summary>
public sealed record NoteResponse(
    Guid Id,
    Guid NoteTypeId,
    Guid EntityId,
    Guid? ParentNoteId,
    string? Caption,
    string Text,
    bool IsAlert,
    bool IsPinned,
    bool IsPrivateNote,
    NoteApprovalState ApprovalStatus,
    DateTimeOffset? ApprovedOnUtc,
    Guid? AuthorUserId,
    DateTimeOffset? EditedOnUtc,
    IReadOnlyList<Guid> AttachmentBinaryFileIds,
    IReadOnlyList<NoteResponse> Replies,
    DateTimeOffset CreatedOnUtc
);
