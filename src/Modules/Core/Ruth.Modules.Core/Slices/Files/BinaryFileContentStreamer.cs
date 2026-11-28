using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Storage;

namespace Ruth.Modules.Core.Slices.Files;

internal static class BinaryFileContentStreamer
{
    public static async Task<IResult> DownloadAsync(
        Guid id,
        CoreDbContext dbContext,
        IStorageProviderResolver providerResolver,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var fileId = BinaryFileId.From(id);
        var file = await dbContext
            .BinaryFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId, cancellationToken);
        if (file is null)
        {
            return TypedResults.NotFound();
        }

        var provider = providerResolver.Resolve(file.StorageProviderKey);
        if (provider is null)
        {
            return TypedResults.NotFound();
        }

        var stream = await provider.OpenReadAsync(file.ObjectKey, cancellationToken);
        if (stream is null)
        {
            return TypedResults.NotFound();
        }

        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        httpContext.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        httpContext.Response.Headers.CacheControl = "private, no-store";

        var safeFileName = string.IsNullOrWhiteSpace(file.FileName)
            ? $"file-{file.Id.Value:N}.bin"
            : file.FileName;
        return TypedResults.Stream(stream, file.MimeType, safeFileName);
    }
}
