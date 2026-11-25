using System.Collections.Concurrent;
using System.Globalization;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Infrastructure.Storage;

/// <summary>Fallback in-memory <see cref="IStorageProvider" /> for tests and local environments without RustFS.</summary>
internal sealed class InMemoryStorageProvider(IClock clock) : IStorageProvider
{
    public const string Key = "in-memory";

    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _objects =
        new(StringComparer.Ordinal);

    public string ProviderKey => Key;

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
        var normalizedKey = objectKey.TrimStart('/');
        _objects[normalizedKey] = (bytes, contentType);
        return new StorageObjectMetadata(normalizedKey, bytes.LongLength, contentType, ETag: null);
    }

    public Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedKey = objectKey.TrimStart('/');
        return Task.FromResult<Stream?>(
            _objects.TryGetValue(normalizedKey, out var entry)
                ? new MemoryStream(entry.Bytes, writable: false)
                : null
        );
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_objects.ContainsKey(objectKey.TrimStart('/')));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = _objects.TryRemove(objectKey.TrimStart('/'), out _);
        return Task.CompletedTask;
    }

    public Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expiresAt = clock.UtcNow.Add(expiresIn);
        var url = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"https://in-memory.ruth.local/{objectKey.TrimStart('/')}?expires={expiresAt.ToUnixTimeSeconds()}"
            )
        );
        return Task.FromResult(new StoragePresignedUrl(url, expiresAt, "GET"));
    }

    public Task<StoragePresignedUrl> CreatePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var expiresAt = clock.UtcNow.Add(expiresIn);
        var url = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"https://in-memory.ruth.local/{objectKey.TrimStart('/')}?method=PUT&expires={expiresAt.ToUnixTimeSeconds()}"
            )
        );
        return Task.FromResult(new StoragePresignedUrl(url, expiresAt, "PUT"));
    }
}
