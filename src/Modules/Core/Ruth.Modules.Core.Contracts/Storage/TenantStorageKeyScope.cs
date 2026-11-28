using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Core.Contracts.Storage;

/// <summary>
/// Enforces mandatory tenants/{TenantId}/... S3/object storage key namespacing
/// and blocks path-traversal or cross-tenant key access across all <see cref="IStorageProvider" /> implementations.
/// </summary>
public static class TenantStorageKeyScope
{
    private const string TenantsPrefix = "tenants/";

    /// <summary>Validates <paramref name="rawKey" /> and scopes it to the active <paramref name="tenantContext" />.</summary>
    public static string ResolveKey(ITenantContext tenantContext, string rawKey)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);
        return ResolveKey(tenantContext.TenantId, rawKey);
    }

    /// <summary>Validates <paramref name="rawKey" /> and scopes it to <paramref name="tenantId" />.</summary>
    public static string ResolveKey(TenantId tenantId, string rawKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawKey);

        if (tenantId.IsEmpty)
        {
            throw new TenantContextRequiredException(
                "Storage object operations require an active tenant context."
            );
        }

        foreach (var ch in rawKey)
        {
            if (char.IsControl(ch))
            {
                throw new CrossTenantAccessException(
                    "Storage object key cannot contain control characters."
                );
            }
        }

        var normalized = rawKey.Replace('\\', '/').Trim().TrimStart('/');
        if (
            string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("..", StringComparison.Ordinal)
        )
        {
            throw new CrossTenantAccessException(
                "Storage object key cannot contain path traversal segments."
            );
        }

        var expectedPrefix = $"{TenantsPrefix}{tenantId}/";
        if (normalized.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var suffix = normalized[expectedPrefix.Length..].TrimStart('/');
            return string.IsNullOrWhiteSpace(suffix)
                ? throw new CrossTenantAccessException(
                    "Storage object key must specify an object path beneath the tenant prefix."
                )
                : $"{expectedPrefix}{suffix}";
        }

        return normalized.StartsWith(TenantsPrefix, StringComparison.OrdinalIgnoreCase)
            ? throw new CrossTenantAccessException(
                $"Storage object key '{normalized}' belongs to a different tenant than '{tenantId}'."
            )
            : $"{expectedPrefix}{normalized}";
    }
}
