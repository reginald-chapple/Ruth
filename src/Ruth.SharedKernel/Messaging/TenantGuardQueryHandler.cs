using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Messaging;

/// <summary>CQRS decorator verifying tenant context and principal tenant binding before executing query handlers.</summary>
public sealed class TenantGuardQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TQuery));

    /// <inheritdoc />
    public Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!_isControlPlane)
        {
            if (!tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Query '{typeof(TQuery).Name}' requires an active tenant context."
                );
            }

            principalValidator?.EnsurePrincipalMatchesTenant(tenantContext);
        }

        return inner.HandleAsync(query, cancellationToken);
    }
}
