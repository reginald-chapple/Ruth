using System.Collections.Concurrent;
using System.Text;
using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Modules.VirtualRooms.Tests.Fixtures;

internal sealed class VirtualRoomsRecordingStorageSpy : IStorageProvider
{
    public ConcurrentDictionary<string, string> UploadedUtf8Payloads { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public string ProviderKey => "spy-s3";

    public async Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        UploadedUtf8Payloads[objectKey] = Encoding.UTF8.GetString(bytes);
        return new StorageObjectMetadata(objectKey, bytes.Length, contentType, ETag: "\"etag\"");
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
        return Task.FromResult(UploadedUtf8Payloads.ContainsKey(objectKey));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _ = UploadedUtf8Payloads.TryRemove(objectKey, out _);
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
                new Uri($"https://rustfs.local/{objectKey}"),
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
                new Uri($"https://rustfs.local/upload/{objectKey}"),
                DateTimeOffset.UtcNow.Add(expiresIn),
                "PUT"
            )
        );
    }
}
