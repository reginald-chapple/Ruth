using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CreateBinaryFileTypeCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateBinaryFileTypeCommand, BinaryFileTypeResponse>
{
    public async Task<Result<BinaryFileTypeResponse>> HandleAsync(
        CreateBinaryFileTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = BinaryFileType.Create(
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
        if (created.IsFailure)
        {
            return Result.Failure<BinaryFileTypeResponse>(created.Errors);
        }

        var exists = await dbContext.BinaryFileTypes.AnyAsync(
            t => t.NormalizedName == created.Value.NormalizedName,
            cancellationToken
        );
        if (exists)
        {
            return CoreDocumentErrors.BinaryFileTypeDuplicateName;
        }

        _ = dbContext.BinaryFileTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(created.Value));
    }
}
