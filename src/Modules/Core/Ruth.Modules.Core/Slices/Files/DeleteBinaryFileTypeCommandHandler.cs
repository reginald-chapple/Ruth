using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class DeleteBinaryFileTypeCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteBinaryFileTypeCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteBinaryFileTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = BinaryFileTypeId.From(command.BinaryFileTypeId);
        var entity = await dbContext.BinaryFileTypes.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CoreDocumentErrors.BinaryFileTypeNotFound;
        }

        if (entity.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var inUse = await dbContext.BinaryFiles.AnyAsync(
            f => f.BinaryFileTypeId == id,
            cancellationToken
        );
        if (inUse)
        {
            return CoreDocumentErrors.BinaryFileTypeInUse;
        }

        _ = dbContext.BinaryFileTypes.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
