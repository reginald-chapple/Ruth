using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Enforces that any authenticated HTTP or Blazor circuit security principal carries a tenant claim matching
/// the active <see cref="ITenantContext" /> before Application CQRS handlers execute.
/// </summary>
internal sealed class HttpContextPrincipalTenantValidator(IHttpContextAccessor httpContextAccessor)
    : IPrincipalTenantValidator
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor
        ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public void EnsurePrincipalMatchesTenant(ITenantContext tenantContext)
    {
        ArgumentNullException.ThrowIfNull(tenantContext);

        var user = _httpContextAccessor.HttpContext?.User
            ?? AmbientCircuitSecurityContext.CurrentPrincipal;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var isOperatorPrincipal = user.HasClaim("platform_operator", "true");
        if (tenantContext.IsPlatformOperatorHost)
        {
            if (!isOperatorPrincipal)
            {
                throw new CrossTenantAccessException(
                    "Tenant user credentials cannot execute on the Platform Operator host."
                );
            }

            return;
        }

        if (isOperatorPrincipal)
        {
            throw new CrossTenantAccessException(
                "Platform Operator credentials cannot execute inside a tenant workspace."
            );
        }

        if (!tenantContext.HasTenant)
        {
            return;
        }

        var tidClaim = user.FindFirst("tid")?.Value;
        if (
            !Guid.TryParse(tidClaim, out var claimGuid)
            || claimGuid != tenantContext.TenantId.Value
        )
        {
            throw new CrossTenantAccessException(
                "Authenticated principal tenant claim is missing or does not match the active workspace."
            );
        }
    }
}
