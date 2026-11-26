using System.Collections.Concurrent;
using Ruth.Modules.Core.Contracts.Storage;

namespace Ruth.Modules.Events.Tests.Fixtures;

internal sealed class EventsInMemoryStorageProvider : IStorageProvider
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

    public string ProviderKey => "in-memory-events-test";

    public async Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(content);

        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        _objects[objectKey] = bytes;
        return new StorageObjectMetadata(objectKey, bytes.LongLength, contentType, "etag-events");
    }

    public Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        if (!_objects.TryGetValue(objectKey, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream copy = new MemoryStream([.. bytes], writable: false);
        return Task.FromResult<Stream?>(copy);
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_objects.ContainsKey(objectKey));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _ = _objects.TryRemove(objectKey, out _);
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
                new Uri($"https://storage.ruth.test/{objectKey}"),
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
                new Uri($"https://storage.ruth.test/{objectKey}"),
                DateTimeOffset.UtcNow.Add(expiresIn),
                "PUT"
            )
        );
    }
}
