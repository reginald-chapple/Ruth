using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Modules.Engagement.Tests.Fixtures;

internal sealed class EngagementInMemoryStorageProvider : IStorageProvider
{
    public string ProviderKey => "in-memory";

    public async Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        return new StorageObjectMetadata(objectKey, ms.Length, contentType, ETag: "\"etag\"");
    }

    public Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<Stream?>(new MemoryStream());
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            new StoragePresignedUrl(
                new Uri($"https://storage.local/{objectKey}"),
                DateTimeOffset.UtcNow.Add(expiresIn),
                "GET"
            )
        );
    }

    public Task<StoragePresignedUrl> CreatePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(
            new StoragePresignedUrl(
                new Uri($"https://storage.local/upload/{objectKey}"),
                DateTimeOffset.UtcNow.Add(expiresIn),
                "PUT"
            )
        );
    }
}
