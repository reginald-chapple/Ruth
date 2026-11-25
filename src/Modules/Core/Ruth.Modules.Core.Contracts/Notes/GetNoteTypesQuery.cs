using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Notes;

/// <summary>Lists note types optionally filtered by entity type.</summary>
public sealed record GetNoteTypesQuery(Guid? EntityTypeId = null)
    : IQuery<IReadOnlyList<NoteTypeResponse>>;
