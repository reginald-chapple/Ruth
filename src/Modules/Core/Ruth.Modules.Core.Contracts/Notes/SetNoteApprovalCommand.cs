using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Approves or denies a note pending moderation.</summary>
public sealed record SetNoteApprovalCommand(Guid NoteId, NoteApprovalState ApprovalStatus)
    : ICommand<NoteResponse>;
