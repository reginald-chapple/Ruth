using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class CreateNoteTypeCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateNoteTypeCommand, NoteTypeResponse>
{
    public async Task<Result<NoteTypeResponse>> HandleAsync(
        CreateNoteTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = NoteType.Create(
            EntityTypeDescriptorId.From(command.EntityTypeId),
            command.Name,
            command.UserSelectable,
            command.IconName,
            command.Color,
            command.Order,
            command.RequiresApprovals,
            command.AllowsWatching,
            command.AllowsReplies,
            command.MaxReplyDepth,
            command.AutoWatchAuthors,
            command.AllowsAttachments,
            command.BinaryFileTypeId.HasValue
                ? BinaryFileTypeId.From(command.BinaryFileTypeId.Value)
                : null,
            command.IsMentionEnabled
        );
        if (created.IsFailure)
        {
            return Result.Failure<NoteTypeResponse>(created.Errors);
        }

        _ = dbContext.NoteTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(NoteMapper.ToResponse(created.Value));
    }
}
