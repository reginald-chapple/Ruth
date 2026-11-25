using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Storage;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class DeleteBinaryFileCommandHandler(
    CoreDbContext dbContext,
    IStorageProviderResolver providerResolver
) : ICommandHandler<DeleteBinaryFileCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteBinaryFileCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = BinaryFileId.From(command.BinaryFileId);
        var file = await dbContext.BinaryFiles.FirstOrDefaultAsync(
            f => f.Id == id,
            cancellationToken
        );
        if (file is null)
        {
            return CoreDocumentErrors.BinaryFileNotFound;
        }

        if (file.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var provider = providerResolver.Resolve(file.StorageProviderKey);
        if (provider is not null)
        {
            await provider.DeleteAsync(file.ObjectKey, cancellationToken);
        }

        _ = dbContext.BinaryFiles.Remove(file);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
