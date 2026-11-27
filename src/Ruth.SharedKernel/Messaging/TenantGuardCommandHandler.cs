using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Messaging;

/// <summary>CQRS decorator verifying tenant context, lifecycle eligibility, and principal tenant binding before executing command handlers.</summary>
public sealed class TenantGuardCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TCommand));

    private readonly ICommandHandler<TCommand> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IPrincipalTenantValidator? _principalValidator = principalValidator;

    /// <inheritdoc />
    public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_isControlPlane)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Command '{typeof(TCommand).Name}' requires an active tenant context."
                );
            }

            if (_tenantContext.LifecycleStage == TenantLifecycleStage.Suspended)
            {
                throw new CrossTenantAccessException(
                    $"Command '{typeof(TCommand).Name}' cannot execute because tenant '{_tenantContext.TenantId}' is suspended."
                );
            }
        }

        _principalValidator?.EnsurePrincipalMatchesTenant(_tenantContext);
        return _inner.HandleAsync(command, cancellationToken);
    }
}

/// <summary>CQRS decorator verifying tenant context, lifecycle eligibility, and principal tenant binding before executing command handlers returning <typeparamref name="TResponse" />.</summary>
public sealed class TenantGuardCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TCommand));

    private readonly ICommandHandler<TCommand, TResponse> _inner = inner
        ?? throw new ArgumentNullException(nameof(inner));
    private readonly ITenantContext _tenantContext = tenantContext
        ?? throw new ArgumentNullException(nameof(tenantContext));
    private readonly IPrincipalTenantValidator? _principalValidator = principalValidator;

    /// <inheritdoc />
    public Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_isControlPlane)
        {
            if (!_tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Command '{typeof(TCommand).Name}' requires an active tenant context."
                );
            }

            if (_tenantContext.LifecycleStage == TenantLifecycleStage.Suspended)
            {
                throw new CrossTenantAccessException(
                    $"Command '{typeof(TCommand).Name}' cannot execute because tenant '{_tenantContext.TenantId}' is suspended."
                );
            }
        }

        _principalValidator?.EnsurePrincipalMatchesTenant(_tenantContext);
        return _inner.HandleAsync(command, cancellationToken);
    }
}
