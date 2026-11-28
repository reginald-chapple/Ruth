namespace Ruth.SharedKernel.Tenancy;

/// <summary>
/// Resolves verified canonical HTTPS origins and absolute links for tenant workspaces without trusting
/// inbound HTTP <c lang="text">Host</c> or <c lang="text">X-Forwarded-Host</c> headers.
/// </summary>
public interface ICanonicalTenantUrlProvider
{
    /// <summary>Returns the canonical <c lang="text">https://&lt;verified-host&gt;</c> base URL (without trailing slash) for <paramref name="tenantId" />, or empty when unavailable.</summary>
    ValueTask<string> GetCanonicalBaseUrlAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Builds a canonical absolute HTTPS URL combining the verified tenant origin with <paramref name="relativePathAndQuery" />.</summary>
    ValueTask<string> BuildTenantUrlAsync(
        TenantId tenantId,
        string relativePathAndQuery,
        CancellationToken cancellationToken = default
    );
}
