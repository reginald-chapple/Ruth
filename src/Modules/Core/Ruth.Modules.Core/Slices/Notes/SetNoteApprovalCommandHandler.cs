using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class SetNoteApprovalCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<SetNoteApprovalCommand, NoteResponse>
{
    public async Task<Result<NoteResponse>> HandleAsync(
        SetNoteApprovalCommand command,
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

        note.SetApprovalStatus(command.ApprovalStatus, clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(NoteMapper.ToResponse(note));
    }
}
