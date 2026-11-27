using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Messaging;

/// <summary>CQRS decorator verifying tenant context, lifecycle eligibility, and principal tenant binding before executing query handlers.</summary>
public sealed class TenantGuardQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TQuery));

    private readonly IQueryHandler<TQuery, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IPrincipalTenantValidator? _principalValidator = principalValidator;

    /// <inheritdoc />
    public Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!_isControlPlane)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Query '{typeof(TQuery).Name}' requires an active tenant context."
                );
            }

            if (_tenantContext.LifecycleStage == TenantLifecycleStage.Suspended)
            {
                throw new CrossTenantAccessException(
                    $"Query '{typeof(TQuery).Name}' cannot execute because tenant '{_tenantContext.TenantId}' is suspended."
                );
            }
        }

        _principalValidator?.EnsurePrincipalMatchesTenant(_tenantContext);
        return _inner.HandleAsync(query, cancellationToken);
    }
}
