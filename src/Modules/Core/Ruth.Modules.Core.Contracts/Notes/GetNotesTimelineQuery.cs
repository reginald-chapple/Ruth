using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Gets the threaded notes timeline for an entity with privacy and approval visibility enforced.</summary>
public sealed record GetNotesTimelineQuery(
    Guid EntityId,
    Guid? NoteTypeId = null,
    Guid? ViewerUserId = null,
    bool IncludeUnapproved = false
) : IQuery<IReadOnlyList<NoteResponse>>;
