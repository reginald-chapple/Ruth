namespace Ruth.SharedKernel.Tenancy;

/// <summary>
/// Fallback canonical URL provider used in isolated module unit tests when the Tenancy control-plane module is not registered.
/// </summary>
internal sealed class NullCanonicalTenantUrlProvider : ICanonicalTenantUrlProvider
{
    public ValueTask<string> GetCanonicalBaseUrlAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var slug = AmbientTenantContext.CurrentState.TenantSlug;
        var baseUrl = string.IsNullOrWhiteSpace(slug)
            ? string.Empty
            : $"https://{slug.Trim().ToLowerInvariant()}.sandbox.localhost";
        return ValueTask.FromResult(baseUrl);
    }

    public async ValueTask<string> BuildTenantUrlAsync(
        TenantId tenantId,
        string relativePathAndQuery,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePathAndQuery);
        var normalizedPath = relativePathAndQuery.StartsWith('/')
            ? relativePathAndQuery
            : $"/{relativePathAndQuery}";
        var baseUrl = await GetCanonicalBaseUrlAsync(tenantId, cancellationToken);
        return string.IsNullOrEmpty(baseUrl) ? normalizedPath : $"{baseUrl}{normalizedPath}";
    }
}
