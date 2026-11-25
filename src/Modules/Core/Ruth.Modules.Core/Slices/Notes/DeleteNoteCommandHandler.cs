using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class DeleteNoteCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteNoteCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteNoteCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = NoteId.From(command.NoteId);
        var note = await dbContext
            .Notes.Include(static n => n.Attachments)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (note is null)
        {
            return CoreCollaborationErrors.NoteNotFound;
        }

        if (!note.CanBeModifiedBy(command.RequestingUserId, command.IsAdmin))
        {
            return CoreCollaborationErrors.NoteAccessDenied;
        }

        var attachmentIds = note.Attachments.Select(static a => a.BinaryFileId).ToList();
        if (attachmentIds.Count > 0)
        {
            var files = await dbContext
                .BinaryFiles.Where(f => attachmentIds.Contains(f.Id))
                .ToListAsync(cancellationToken);
            foreach (var file in files)
            {
                file.MarkTemporary();
            }
        }

        _ = dbContext.Notes.Remove(note);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
