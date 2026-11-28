using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Providers.RustFsStorage;

/// <summary>
/// Hardened S3-compatible <see cref="IStorageProvider" /> backed by RustFS with private-by-default buckets,
/// short-lived presigned URLs, active-content blocking, and non-inline attachment headers.
/// </summary>
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

    private const string PublicAccessBlockXml =
        "<PublicAccessBlockConfiguration xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\">"
        + "<BlockPublicAcls>true</BlockPublicAcls>"
        + "<IgnorePublicAcls>true</IgnorePublicAcls>"
        + "<BlockPublicPolicy>true</BlockPublicPolicy>"
        + "<RestrictPublicBuckets>true</RestrictPublicBuckets>"
        + "</PublicAccessBlockConfiguration>";

    private static readonly IReadOnlyDictionary<string, string> _attachmentDownloadQuery =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["response-content-disposition"] = "attachment",
        };

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

        if (IsProhibitedSvgContentType(contentType))
        {
            throw new InvalidOperationException(
                "Uploading active SVG content to S3/RustFS object storage is prohibited."
            );
        }

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
        request.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(
            "attachment"
        );
        if (opts.EnforcePrivateBucket)
        {
            request.Headers.Add("x-amz-acl", "private");
        }

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
        var maxSeconds = ResolveMaxPresignedSeconds(opts);
        var seconds = ClampPresignedSeconds(expiresIn, maxSeconds);
        var nowUtc = clock.UtcNow;
        var url = AwsSigV4Signer.CreatePresignedUrl(
            new Uri(opts.Endpoint),
            opts.BucketName,
            scopedKey,
            "GET",
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            nowUtc,
            seconds,
            maxSeconds,
            _attachmentDownloadQuery
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
        if (FileContentSecurityInspector.IsBlockedActiveMimeType(contentType))
        {
            throw new InvalidOperationException(
                "Presigned upload URLs cannot be minted for active HTML, SVG, or script MIME types."
            );
        }

        var scopedKey = TenantStorageKeyScope.ResolveKey(AmbientTenantContext.Instance, objectKey);
        var opts = optionsAccessor.Value;
        var maxSeconds = ResolveMaxPresignedSeconds(opts);
        var seconds = ClampPresignedSeconds(expiresIn, maxSeconds);
        var nowUtc = clock.UtcNow;
        var url = AwsSigV4Signer.CreatePresignedUrl(
            new Uri(opts.Endpoint),
            opts.BucketName,
            scopedKey,
            "PUT",
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            nowUtc,
            seconds,
            maxSeconds
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
        if (!headRes.IsSuccessStatusCode)
        {
            await CreatePrivateBucketAsync(client, bucketUri, opts, cancellationToken);
        }

        if (opts.EnforcePrivateBucket)
        {
            await ApplyPublicAccessBlockBestEffortAsync(client, bucketUri, opts, cancellationToken);
        }

        _bucketVerified = true;
    }

    private async Task CreatePrivateBucketAsync(
        HttpClient client,
        Uri bucketUri,
        RustFsStorageOptions opts,
        CancellationToken cancellationToken
    )
    {
        using var putReq = new HttpRequestMessage(HttpMethod.Put, bucketUri);
        if (opts.EnforcePrivateBucket)
        {
            putReq.Headers.Add("x-amz-acl", "private");
        }

        AwsSigV4Signer.SignRequest(
            putReq,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );
        using var putRes = await client.SendAsync(putReq, cancellationToken);
        if (!putRes.IsSuccessStatusCode && putRes.StatusCode != HttpStatusCode.Conflict)
        {
            _ = putRes.EnsureSuccessStatusCode();
        }
    }

    private async Task ApplyPublicAccessBlockBestEffortAsync(
        HttpClient client,
        Uri bucketUri,
        RustFsStorageOptions opts,
        CancellationToken cancellationToken
    )
    {
        var blockUri = new Uri($"{bucketUri}?publicAccessBlock=");
        using var blockReq = new HttpRequestMessage(HttpMethod.Put, blockUri)
        {
            Content = new StringContent(PublicAccessBlockXml, Encoding.UTF8, "application/xml"),
        };
        AwsSigV4Signer.SignRequest(
            blockReq,
            opts.AccessKey,
            opts.SecretKey,
            opts.Region,
            clock.UtcNow
        );
        using var _ = await client.SendAsync(blockReq, cancellationToken);
    }

    private static int ResolveMaxPresignedSeconds(RustFsStorageOptions opts)
    {
        return Math.Clamp(
            opts.MaxPresignedUrlLifetimeSeconds,
            60,
            RustFsStorageOptions.DefaultMaxPresignedLifetimeSeconds
        );
    }

    private static int ClampPresignedSeconds(TimeSpan expiresIn, int maxSeconds)
    {
        return Math.Clamp((int)Math.Ceiling(expiresIn.TotalSeconds), 1, maxSeconds);
    }

    private static bool IsProhibitedSvgContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType)
            && contentType.Contains("svg", StringComparison.OrdinalIgnoreCase);
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
