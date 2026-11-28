using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Infrastructure.Storage;

/// <summary>
/// Decorator enforcing strict per-tenant S3/object key namespacing (tenants/{TenantId}/...)
/// and blocking cross-tenant object traversal across all storage providers.
/// </summary>
internal sealed class TenantScopedStorageProvider(
    IStorageProvider inner,
    ITenantContext tenantContext
) : IStorageProvider
{
    public string ProviderKey => inner.ProviderKey;

    public Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.UploadAsync(scopedKey, content, contentType, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(
        string objectKey,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.OpenReadAsync(scopedKey, cancellationToken);
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.ExistsAsync(scopedKey, cancellationToken);
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.DeleteAsync(scopedKey, cancellationToken);
    }

    public Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.CreatePresignedDownloadUrlAsync(scopedKey, expiresIn, cancellationToken);
    }

    public Task<StoragePresignedUrl> CreatePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    )
    {
        var scopedKey = ScopeAndValidateKey(objectKey);
        return inner.CreatePresignedUploadUrlAsync(
            scopedKey,
            contentType,
            expiresIn,
            cancellationToken
        );
    }

    private string ScopeAndValidateKey(string rawKey)
    {
        return TenantStorageKeyScope.ResolveKey(tenantContext, rawKey);
    }
}
