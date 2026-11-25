using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Creates a new root note or threaded reply with optional attachments and auto-watch.</summary>
public sealed record CreateNoteCommand(
    Guid NoteTypeId,
    Guid EntityId,
    string Text,
    string? Caption = null,
    Guid? ParentNoteId = null,
    bool IsAlert = false,
    bool IsPinned = false,
    bool IsPrivateNote = false,
    Guid? AuthorUserId = null,
    IReadOnlyList<Guid>? AttachmentBinaryFileIds = null
) : ICommand<NoteResponse>;
