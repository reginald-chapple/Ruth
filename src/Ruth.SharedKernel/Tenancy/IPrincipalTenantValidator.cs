namespace Ruth.SharedKernel.Tenancy;

/// <summary>Validates that any authenticated security principal matches the active <see cref="ITenantContext" />.</summary>
public interface IPrincipalTenantValidator
{
    /// <summary>Throws <see cref="CrossTenantAccessException" /> if an authenticated principal is bound to a different tenant.</summary>
    void EnsurePrincipalMatchesTenant(ITenantContext tenantContext);
}
