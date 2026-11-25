using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Deletes a note (and optionally its child replies) enforcing creator/admin checks.</summary>
public sealed record DeleteNoteCommand(
    Guid NoteId,
    Guid? RequestingUserId = null,
    bool IsAdmin = false
) : ICommand<bool>;
