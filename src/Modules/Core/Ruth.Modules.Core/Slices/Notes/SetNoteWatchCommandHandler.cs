using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class SetNoteWatchCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<SetNoteWatchCommand, NoteWatchResponse>
{
    public async Task<Result<NoteWatchResponse>> HandleAsync(
        SetNoteWatchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        NoteTypeId? noteTypeId = command.NoteTypeId.HasValue
            ? NoteTypeId.From(command.NoteTypeId.Value)
            : null;
        EntityTypeDescriptorId? entityTypeId = command.EntityTypeId.HasValue
            ? EntityTypeDescriptorId.From(command.EntityTypeId.Value)
            : null;
        NoteId? noteId = command.NoteId.HasValue ? NoteId.From(command.NoteId.Value) : null;

        var precheck = await ValidateWatchScopeAsync(
            command,
            noteTypeId,
            entityTypeId,
            noteId,
            cancellationToken
        );
        if (precheck.IsFailure)
        {
            return Result.Failure<NoteWatchResponse>(precheck.Errors);
        }

        var created = NoteWatch.Create(
            command.WatcherUserId,
            command.IsWatching,
            command.WatchReplies,
            command.AllowOverride,
            noteTypeId,
            entityTypeId,
            command.EntityId,
            noteId
        );
        if (created.IsFailure)
        {
            return Result.Failure<NoteWatchResponse>(created.Errors);
        }

        _ = dbContext.NoteWatches.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(NoteMapper.ToResponse(created.Value));
    }

    private async Task<Result> ValidateWatchScopeAsync(
        SetNoteWatchCommand command,
        NoteTypeId? noteTypeId,
        EntityTypeDescriptorId? entityTypeId,
        NoteId? noteId,
        CancellationToken cancellationToken
    )
    {
        if (noteTypeId.HasValue)
        {
            var nt = await dbContext
                .NoteTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == noteTypeId.Value, cancellationToken);
            if (nt is null)
            {
                return CoreCollaborationErrors.NoteTypeNotFound;
            }

            if (!nt.AllowsWatching)
            {
                return CoreCollaborationErrors.NoteWatchNotAllowed;
            }
        }

        if (command.IsWatching)
        {
            return Result.Success();
        }

        var blockingWatches = await dbContext
            .NoteWatches.AsNoTracking()
            .Where(w =>
                w.WatcherUserId == command.WatcherUserId && w.IsWatching && !w.AllowOverride
            )
            .ToListAsync(cancellationToken);

        return blockingWatches.Exists(w =>
            w.CoversScope(noteTypeId, entityTypeId, command.EntityId, noteId)
        )
            ? CoreCollaborationErrors.NoteWatchNonOverridable
            : Result.Success();
    }
}
