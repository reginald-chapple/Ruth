using Ruth.Modules.Core.Domain.Files;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Attachment linking a <see cref="Note" /> to a permanent <see cref="BinaryFile" />.</summary>
public sealed class NoteAttachment : Entity<Guid>
{
    private NoteAttachment() { }

    internal NoteAttachment(Guid id, NoteId noteId, BinaryFileId binaryFileId)
        : base(id)
    {
        NoteId = noteId;
        BinaryFileId = binaryFileId;
    }

    /// <summary>Gets the owning note identifier.</summary>
    public NoteId NoteId { get; private set; }

    /// <summary>Gets the attached binary file identifier.</summary>
    public BinaryFileId BinaryFileId { get; private set; }
}
