using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class UpdateBinaryFileTypeCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UpdateBinaryFileTypeCommand, BinaryFileTypeResponse>
{
    public async Task<Result<BinaryFileTypeResponse>> HandleAsync(
        UpdateBinaryFileTypeCommand command,
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

        var updated = entity.Update(
            command.Name,
            command.Description,
            command.IconName,
            command.StorageProviderKey,
            command.RequiresViewSecurity,
            command.AllowAnonymous,
            command.PreferredRequired,
            command.MaxFileSizeBytes,
            command.MaxWidth,
            command.MaxHeight,
            command.AllowedExtensions,
            command.CacheControlHeaderSettings
        );
        if (updated.IsFailure)
        {
            return Result.Failure<BinaryFileTypeResponse>(updated.Errors);
        }

        var duplicate = await dbContext.BinaryFileTypes.AnyAsync(
            t => t.Id != id && t.NormalizedName == entity.NormalizedName,
            cancellationToken
        );
        if (duplicate)
        {
            return CoreDocumentErrors.BinaryFileTypeDuplicateName;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(entity));
    }
}
