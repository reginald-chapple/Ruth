using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Imaging;
using Ruth.Modules.Core.Infrastructure.Storage;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class CropBinaryFileImageCommandHandler(
    CoreDbContext dbContext,
    IStorageProviderResolver providerResolver,
    IImageProcessor imageProcessor,
    IClock clock
) : ICommandHandler<CropBinaryFileImageCommand, BinaryFileResponse>
{
    public async Task<Result<BinaryFileResponse>> HandleAsync(
        CropBinaryFileImageCommand command,
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

        if (!file.IsImage)
        {
            return CoreDocumentErrors.BinaryFileNotAnImage;
        }

        var provider = providerResolver.Resolve(file.StorageProviderKey);
        if (provider is null)
        {
            return CoreDocumentErrors.StorageProviderNotFound;
        }

        var cropped = await LoadAndCropAsync(
            provider,
            file.ObjectKey,
            command.Crop,
            cancellationToken
        );
        return cropped.IsFailure
            ? Result.Failure<BinaryFileResponse>(cropped.Errors)
            : await PersistCroppedResultAsync(
                file,
                provider,
                cropped.Value,
                command.CreateCopy,
                cancellationToken
            );
    }

    private async Task<Result<ProcessedImageResult>> LoadAndCropAsync(
        IStorageProvider provider,
        string objectKey,
        ImageCropRequest crop,
        CancellationToken cancellationToken
    )
    {
        await using var sourceStream = await provider.OpenReadAsync(objectKey, cancellationToken);
        if (sourceStream is null)
        {
            return CoreDocumentErrors.BinaryFileNotFound;
        }

        using var ms = new MemoryStream();
        await sourceStream.CopyToAsync(ms, cancellationToken);
        return imageProcessor.CropAndResize(ms.ToArray(), crop);
    }

    private async Task<Result<BinaryFileResponse>> PersistCroppedResultAsync(
        BinaryFile file,
        IStorageProvider provider,
        ProcessedImageResult processed,
        bool createCopy,
        CancellationToken cancellationToken
    )
    {
        var ext = string.Equals(
            processed.MimeType,
            "image/jpeg",
            StringComparison.OrdinalIgnoreCase
        )
            ? ".jpg"
            : ".png";
        var rawObjectKey =
            $"binary-files/{file.BinaryFileTypeId.Value:N}/{Guid.CreateVersion7():N}/cropped{ext}";
        var newObjectKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            rawObjectKey
        );
        await using var uploadStream = new MemoryStream(processed.Bytes, writable: false);
        _ = await provider.UploadAsync(
            newObjectKey,
            uploadStream,
            processed.MimeType,
            cancellationToken
        );

        if (createCopy)
        {
            return await CreateCroppedCopyAsync(file, newObjectKey, processed, cancellationToken);
        }

        var oldKey = file.ObjectKey;
        file.UpdateStoredContent(
            newObjectKey,
            processed.MimeType,
            processed.Bytes.LongLength,
            processed.Width,
            processed.Height,
            clock.UtcNow
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await provider.DeleteAsync(oldKey, cancellationToken);
        return Result.Success(FileMapper.ToResponse(file));
    }

    private async Task<Result<BinaryFileResponse>> CreateCroppedCopyAsync(
        BinaryFile original,
        string objectKey,
        ProcessedImageResult processed,
        CancellationToken cancellationToken
    )
    {
        var fileType = await dbContext
            .BinaryFileTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == original.BinaryFileTypeId, cancellationToken);
        if (fileType is null)
        {
            return CoreDocumentErrors.BinaryFileTypeNotFound;
        }

        var copyResult = BinaryFile.Create(
            fileType,
            $"cropped_{original.FileName}",
            processed.MimeType,
            processed.Bytes.LongLength,
            original.StorageProviderKey,
            objectKey,
            processed.Width,
            processed.Height,
            original.IsTemporary,
            original.Description,
            original.ParentEntityTypeId,
            original.ParentEntityId,
            original.UploadedByUserId,
            clock.UtcNow
        );
        if (copyResult.IsFailure)
        {
            return Result.Failure<BinaryFileResponse>(copyResult.Errors);
        }

        _ = dbContext.BinaryFiles.Add(copyResult.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(copyResult.Value));
    }
}
