using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.RustFsStorage;

/// <summary>S3-compatible <see cref="IStorageProvider" /> backed by RustFS with SigV4 streaming and presigned URLs.</summary>
public sealed class RustFsStorageProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<RustFsStorageOptions> optionsAccessor,
    IClock clock
) : IStorageProvider
{
    /// <summary>Named <see cref="HttpClient" /> identifier registered in DI.</summary>
    public const string HttpClientName = "Ruth.RustFsStorage";

    /// <summary>Canonical provider key registered for RustFS storage.</summary>
    public const string Key = "rustfs";

    private volatile bool _bucketVerified;

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public async Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(content);

        var opts = optionsAccessor.Value;
        await EnsureBucketExistsAsync(opts, cancellationToken);

        var normalizedKey = TenantStorageKeyScope.ResolveKey(
            AmbientTenantContext.Instance,
            objectKey
        );
        var targetUri = BuildObjectUri(opts, normalizedKey);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Put, targetUri);
        request.Content = new ByteArrayContent(bytes);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        );
        AwsSigV4Signer.SignRequest(
            request,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();

        var etag = response.Headers.ETag?.Tag?.Trim('"');
        return new StorageObjectMetadata(
            normalizedKey,
            bytes.LongLength,
            request.Content.Headers.ContentType.MediaType ?? "application/octet-stream",
            etag
        );
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);

        var opts = optionsAccessor.Value;
        var targetUri = BuildObjectUri(opts, scopedKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
        AwsSigV4Signer.SignRequest(
            request,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        _ = response.EnsureSuccessStatusCode();
        var memory = new MemoryStream();
        await response.Content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);

        var opts = optionsAccessor.Value;
        var targetUri = BuildObjectUri(opts, scopedKey);
        using var request = new HttpRequestMessage(HttpMethod.Head, targetUri);
        AwsSigV4Signer.SignRequest(
            request,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);

        var opts = optionsAccessor.Value;
        var targetUri = BuildObjectUri(opts, scopedKey);
        using var request = new HttpRequestMessage(HttpMethod.Delete, targetUri);
        AwsSigV4Signer.SignRequest(
            request,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            _ = response.EnsureSuccessStatusCode();
        }
    }

    /// <inheritdoc />
    public Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);

        var opts = optionsAccessor.Value;
        var nowUtc = clock.UtcNow;
        var seconds = Math.Clamp((int)Math.Ceiling(expiresIn.TotalSeconds), 1, 604_800);
        var url = AwsSigV4Signer.CreatePresignedUrl(
            new Uri(opts.Endpoint),
            opts.BucketName,
            scopedKey,
            "GET",
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            nowUtc,
            seconds
        );
        return Task.FromResult(new StoragePresignedUrl(url, nowUtc.AddSeconds(seconds), "GET"));
    }

    /// <inheritdoc />
    public Task<StoragePresignedUrl> CreatePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);

        var opts = optionsAccessor.Value;
        var nowUtc = clock.UtcNow;
        var seconds = Math.Clamp((int)Math.Ceiling(expiresIn.TotalSeconds), 1, 604_800);
        var url = AwsSigV4Signer.CreatePresignedUrl(
            new Uri(opts.Endpoint),
            opts.BucketName,
            scopedKey,
            "PUT",
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            nowUtc,
            seconds
        );
        return Task.FromResult(new StoragePresignedUrl(url, nowUtc.AddSeconds(seconds), "PUT"));
    }

    private async Task EnsureBucketExistsAsync(
        RustFsStorageOptions opts,
        CancellationToken cancellationToken
    )
    {
        if (!opts.AutoCreateBucket || _bucketVerified)
        {
            return;
        }

        var bucketUri = BuildBucketUri(opts);
        var client = httpClientFactory.CreateClient(HttpClientName);

        using var headReq = new HttpRequestMessage(HttpMethod.Head, bucketUri);
        AwsSigV4Signer.SignRequest(
            headReq,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );
        using var headRes = await client.SendAsync(headReq, cancellationToken);
        if (headRes.IsSuccessStatusCode)
        {
            _bucketVerified = true;
            return;
        }

        using var putReq = new HttpRequestMessage(HttpMethod.Put, bucketUri);
        AwsSigV4Signer.SignRequest(
            putReq,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );
        using var putRes = await client.SendAsync(putReq, cancellationToken);
        if (putRes.IsSuccessStatusCode || putRes.StatusCode == HttpStatusCode.Conflict)
        {
            _bucketVerified = true;
        }
        else
        {
            _ = putRes.EnsureSuccessStatusCode();
        }
    }

    private static Uri BuildBucketUri(RustFsStorageOptions opts)
    {
        var baseUri = opts.Endpoint.TrimEnd('/');
        return new Uri($"{baseUri}/{opts.BucketName.Trim('/')}");
    }

    private static Uri BuildObjectUri(RustFsStorageOptions opts, string normalizedKey)
    {
        var baseUri = opts.Endpoint.TrimEnd('/');
        var encodedSegments = string.Join(
            '/',
            normalizedKey.Split('/').Select(Uri.EscapeDataString)
        );
        return new Uri($"{baseUri}/{opts.BucketName.Trim('/')}/{encodedSegments}");
    }
}
