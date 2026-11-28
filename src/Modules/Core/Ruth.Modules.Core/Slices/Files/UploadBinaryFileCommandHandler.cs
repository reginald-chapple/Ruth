using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Imaging;
using Ruth.Modules.Core.Infrastructure.Storage;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class UploadBinaryFileCommandHandler(
    CoreDbContext dbContext,
    IStorageProviderResolver providerResolver,
    IImageProcessor imageProcessor,
    IClock clock
) : ICommandHandler<UploadBinaryFileCommand, BinaryFileResponse>
{
    public async Task<Result<BinaryFileResponse>> HandleAsync(
        UploadBinaryFileCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = BinaryFileTypeId.From(command.BinaryFileTypeId);
        var fileType = await dbContext
            .BinaryFileTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken);
        if (fileType is null)
        {
            return CoreDocumentErrors.BinaryFileTypeNotFound;
        }

        var scrubbedName = BinaryFileNamePolicy.ScrubAndValidate(
            command.FileName,
            fileType.AllowedExtensions
        );
        if (scrubbedName.IsFailure)
        {
            return Result.Failure<BinaryFileResponse>(scrubbedName.Errors);
        }

        var provider = providerResolver.Resolve(fileType.StorageProviderKey);
        if (provider is null)
        {
            return CoreDocumentErrors.StorageProviderNotFound;
        }

        var preparedResult = await InspectAndPreparePayloadAsync(
            command,
            scrubbedName.Value,
            fileType,
            cancellationToken
        );
        return preparedResult.IsFailure
            ? Result.Failure<BinaryFileResponse>(preparedResult.Errors)
            : await PersistValidatedBinaryFileAsync(
                command,
                fileType,
                scrubbedName.Value,
                provider,
                preparedResult.Value,
                cancellationToken
            );
    }

    private async Task<Result<BinaryFileResponse>> PersistValidatedBinaryFileAsync(
        UploadBinaryFileCommand command,
        BinaryFileType fileType,
        string scrubbedFileName,
        IStorageProvider provider,
        (byte[] Bytes, string MimeType, int? Width, int? Height) prepared,
        CancellationToken cancellationToken
    )
    {
        var rawObjectKey =
            $"binary-files/{fileType.Id.Value:N}/{Guid.CreateVersion7():N}/{scrubbedFileName}";
        var objectKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            rawObjectKey
        );
        var fileResult = CreateFileAggregate(
            command,
            fileType,
            scrubbedFileName,
            provider.ProviderKey,
            objectKey,
            prepared
        );
        if (fileResult.IsFailure)
        {
            return Result.Failure<BinaryFileResponse>(fileResult.Errors);
        }

        await using var uploadStream = new MemoryStream(prepared.Bytes, writable: false);
        _ = await provider.UploadAsync(
            objectKey,
            uploadStream,
            prepared.MimeType,
            cancellationToken
        );

        _ = dbContext.BinaryFiles.Add(fileResult.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(FileMapper.ToResponse(fileResult.Value));
    }

    private async Task<
        Result<(byte[] Bytes, string MimeType, int? Width, int? Height)>
    > InspectAndPreparePayloadAsync(
        UploadBinaryFileCommand command,
        string scrubbedFileName,
        BinaryFileType fileType,
        CancellationToken cancellationToken
    )
    {
        using var buffer = new MemoryStream();
        await command.Content.CopyToAsync(buffer, cancellationToken);
        var payload = buffer.ToArray();

        var inspection = FileContentSecurityInspector.InspectAndValidateUpload(
            payload,
            scrubbedFileName,
            command.MimeType
        );
        if (!inspection.IsValid)
        {
            return DomainError.Validation(
                inspection.FailureCode ?? "Core.BinaryFile.InvalidContent",
                inspection.FailureReason ?? "Uploaded file failed content security validation."
            );
        }

        var canonicalMime = inspection.CanonicalMimeType;
        return canonicalMime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? ProcessImagePayload(payload, canonicalMime, fileType)
            : (payload, canonicalMime, null, null);
    }

    private Result<BinaryFile> CreateFileAggregate(
        UploadBinaryFileCommand command,
        BinaryFileType fileType,
        string scrubbedFileName,
        string providerKey,
        string objectKey,
        (byte[] Bytes, string MimeType, int? Width, int? Height) prepared
    )
    {
        return BinaryFile.Create(
            fileType,
            scrubbedFileName,
            prepared.MimeType,
            prepared.Bytes.LongLength,
            providerKey,
            objectKey,
            prepared.Width,
            prepared.Height,
            command.IsTemporary,
            command.Description,
            command.ParentEntityTypeId.HasValue
                ? EntityTypeDescriptorId.From(command.ParentEntityTypeId.Value)
                : null,
            command.ParentEntityId,
            command.UploadedByUserId,
            clock.UtcNow
        );
    }

    private (byte[] Bytes, string MimeType, int? Width, int? Height) ProcessImagePayload(
        byte[] payload,
        string mimeType,
        BinaryFileType fileType
    )
    {
        if (fileType.MaxWidth.HasValue || fileType.MaxHeight.HasValue)
        {
            var resized = imageProcessor.ResizeToBoundsIfNeeded(
                payload,
                mimeType,
                fileType.MaxWidth,
                fileType.MaxHeight
            );
            if (resized.IsSuccess)
            {
                return (
                    resized.Value.Bytes,
                    resized.Value.MimeType,
                    resized.Value.Width,
                    resized.Value.Height
                );
            }
        }

        var dims = imageProcessor.TryReadDimensions(payload);
        return dims.IsSuccess
            ? (payload, mimeType, dims.Value.Width, dims.Value.Height)
            : (payload, mimeType, null, null);
    }
}
