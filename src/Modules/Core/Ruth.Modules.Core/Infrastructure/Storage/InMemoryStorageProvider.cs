using System.Collections.Concurrent;
using System.Globalization;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Infrastructure.Storage;

/// <summary>Fallback in-memory <see cref="IStorageProvider" /> for tests and local environments without RustFS.</summary>
internal sealed class InMemoryStorageProvider(IClock clock) : IStorageProvider
{
    public const string Key = "in-memory";

    private const int MaxPresignedLifetimeSeconds = 900;

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
        if (
            !string.IsNullOrWhiteSpace(contentType)
            && contentType.Contains("svg", StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                "Uploading active SVG content to object storage is prohibited."
            );
        }

        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        _objects[scopedKey] = (bytes, contentType);
        return new StorageObjectMetadata(scopedKey, bytes.LongLength, contentType, ETag: null);
    }

    public Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        return Task.FromResult<Stream?>(
            _objects.TryGetValue(scopedKey, out var entry)
                ? new MemoryStream(entry.Bytes, writable: false)
                : null
        );
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        return Task.FromResult(_objects.ContainsKey(scopedKey));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        _ = _objects.TryRemove(scopedKey, out _);
        return Task.CompletedTask;
    }

    public Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        var boundedSeconds = Math.Clamp(
            (int)Math.Ceiling(expiresIn.TotalSeconds),
            1,
            MaxPresignedLifetimeSeconds
        );
        var expiresAt = clock.UtcNow.AddSeconds(boundedSeconds);
        var url = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"https://storage.internal.local/{scopedKey}?expires={expiresAt.ToUnixTimeSeconds()}&response-content-disposition=attachment"
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
        if (FileContentSecurityInspector.IsBlockedActiveMimeType(contentType))
        {
            throw new InvalidOperationException(
                "Presigned upload URLs cannot be minted for active HTML, SVG, or script MIME types."
            );
        }

        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        var boundedSeconds = Math.Clamp(
            (int)Math.Ceiling(expiresIn.TotalSeconds),
            1,
            MaxPresignedLifetimeSeconds
        );
        var expiresAt = clock.UtcNow.AddSeconds(boundedSeconds);
        var url = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"https://storage.internal.local/{scopedKey}?method=PUT&expires={expiresAt.ToUnixTimeSeconds()}"
            )
        );
        return Task.FromResult(new StoragePresignedUrl(url, expiresAt, "PUT"));
    }
}
