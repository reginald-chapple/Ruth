using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Storage;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetBinaryFilePresignedUrlQueryHandler(
    CoreDbContext dbContext,
    IStorageProviderResolver providerResolver
) : IQueryHandler<GetBinaryFilePresignedUrlQuery, StoragePresignedUrl>
{
    public async Task<Result<StoragePresignedUrl>> HandleAsync(
        GetBinaryFilePresignedUrlQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = BinaryFileId.From(query.BinaryFileId);
        var file = await dbContext
            .BinaryFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (file is null)
        {
            return CoreDocumentErrors.BinaryFileNotFound;
        }

        var provider = providerResolver.Resolve(file.StorageProviderKey);
        if (provider is null)
        {
            return CoreDocumentErrors.StorageProviderNotFound;
        }

        var boundedMinutes = Math.Clamp(query.ExpiresInMinutes, 1, 1440);
        var presigned = await provider.CreatePresignedDownloadUrlAsync(
            file.ObjectKey,
            TimeSpan.FromMinutes(boundedMinutes),
            cancellationToken
        );
        return Result.Success(presigned);
    }
}
