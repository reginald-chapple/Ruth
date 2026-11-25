using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class DeleteDocumentCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteDocumentCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = DocumentId.From(command.DocumentId);
        var document = await dbContext.Documents.FirstOrDefaultAsync(
            d => d.Id == id,
            cancellationToken
        );
        if (document is null)
        {
            return CoreDocumentErrors.DocumentNotFound;
        }

        if (document.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var binaryFile = await dbContext.BinaryFiles.FirstOrDefaultAsync(
            f => f.Id == document.BinaryFileId,
            cancellationToken
        );
        binaryFile?.MarkTemporary();

        _ = dbContext.Documents.Remove(document);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
