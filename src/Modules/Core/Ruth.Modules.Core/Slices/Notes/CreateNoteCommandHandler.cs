using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class CreateNoteCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<CreateNoteCommand, NoteResponse>
{
    public async Task<Result<NoteResponse>> HandleAsync(
        CreateNoteCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = NoteTypeId.From(command.NoteTypeId);
        var noteType = await dbContext
            .NoteTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken);
        if (noteType is null)
        {
            return CoreCollaborationErrors.NoteTypeNotFound;
        }

        NoteId? parentId = command.ParentNoteId.HasValue
            ? NoteId.From(command.ParentNoteId.Value)
            : null;
        if (parentId.HasValue)
        {
            var replyCheck = await ValidateReplyAsync(
                noteType,
                command.EntityId,
                parentId.Value,
                cancellationToken
            );
            if (replyCheck.IsFailure)
            {
                return Result.Failure<NoteResponse>(replyCheck.Errors);
            }
        }

        var created = Note.Create(
            noteType,
            command.EntityId,
            command.Text,
            command.Caption,
            parentId,
            command.IsAlert,
            command.IsPinned,
            command.IsPrivateNote,
            command.AuthorUserId,
            clock.UtcNow
        );
        if (created.IsFailure)
        {
            return Result.Failure<NoteResponse>(created.Errors);
        }

        var attachResult = await AttachFilesAsync(
            created.Value,
            noteType,
            command.AttachmentBinaryFileIds,
            command.AuthorUserId,
            cancellationToken
        );
        if (attachResult.IsFailure)
        {
            return Result.Failure<NoteResponse>(attachResult.Errors);
        }

        _ = dbContext.Notes.Add(created.Value);
        TryAddAutoWatch(noteType, command.EntityId, created.Value.Id, command.AuthorUserId);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(NoteMapper.ToResponse(created.Value));
    }

    private void TryAddAutoWatch(
        NoteType noteType,
        Guid entityId,
        NoteId noteId,
        Guid? authorUserId
    )
    {
        if (!noteType.AutoWatchAuthors || !authorUserId.HasValue)
        {
            return;
        }

        var autoWatch = NoteWatch.Create(
            authorUserId.Value,
            isWatching: true,
            watchReplies: true,
            allowOverride: true,
            noteType.Id,
            noteType.EntityTypeId,
            entityId,
            noteId
        );
        if (autoWatch.IsSuccess)
        {
            _ = dbContext.NoteWatches.Add(autoWatch.Value);
        }
    }

    private async Task<Result> ValidateReplyAsync(
        NoteType noteType,
        Guid entityId,
        NoteId parentNoteId,
        CancellationToken cancellationToken
    )
    {
        if (!noteType.AllowsReplies)
        {
            return CoreCollaborationErrors.NoteRepliesDisabled;
        }

        var notes = await dbContext
            .Notes.AsNoTracking()
            .Where(n => n.EntityId == entityId)
            .Select(static n => new
            {
                n.Id,
                n.ParentNoteId,
                n.NoteTypeId,
            })
            .ToListAsync(cancellationToken);

        var map = notes.ToDictionary(static n => n.Id);
        if (!map.TryGetValue(parentNoteId, out var parent) || parent.NoteTypeId != noteType.Id)
        {
            return CoreCollaborationErrors.NoteParentMismatch;
        }

        var depth = 1;
        var cursor = parent.ParentNoteId;
        while (cursor.HasValue && map.TryGetValue(cursor.Value, out var ancestor))
        {
            depth++;
            cursor = ancestor.ParentNoteId;
        }

        return noteType.MaxReplyDepth.HasValue && depth > noteType.MaxReplyDepth.Value
            ? CoreCollaborationErrors.NoteReplyDepthExceeded
            : Result.Success();
    }

    private async Task<Result> AttachFilesAsync(
        Note note,
        NoteType noteType,
        IReadOnlyList<Guid>? attachmentIds,
        Guid? authorUserId,
        CancellationToken cancellationToken
    )
    {
        if (!noteType.AllowsAttachments || attachmentIds is not { Count: > 0 })
        {
            return Result.Success();
        }

        var ids = attachmentIds.Distinct().Select(BinaryFileId.From).ToList();
        var files = await dbContext
            .BinaryFiles.Where(f => ids.Contains(f.Id))
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            if (!file.CanBeAttachedBy(authorUserId))
            {
                return CoreDocumentErrors.BinaryFileOwnershipDenied;
            }

            file.MarkPermanent();
            note.AddAttachment(file.Id);
        }

        return Result.Success();
    }
}
