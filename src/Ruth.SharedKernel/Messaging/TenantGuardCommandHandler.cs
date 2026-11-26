using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Messaging;

/// <summary>CQRS decorator verifying tenant context and principal tenant binding before executing command handlers.</summary>
public sealed class TenantGuardCommandHandler<TCommand>(
    ICommandHandler<TCommand> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TCommand));

    /// <inheritdoc />
    public Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_isControlPlane)
        {
            if (!tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Command '{typeof(TCommand).Name}' requires an active tenant context."
                );
            }

            principalValidator?.EnsurePrincipalMatchesTenant(tenantContext);
        }

        return inner.HandleAsync(command, cancellationToken);
    }
}

/// <summary>CQRS decorator verifying tenant context and principal tenant binding before executing command handlers returning <typeparamref name="TResponse" />.</summary>
public sealed class TenantGuardCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    ITenantContext tenantContext,
    IPrincipalTenantValidator? principalValidator = null
) : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private static readonly bool _isControlPlane =
        typeof(IPlatformControlPlaneRequest).IsAssignableFrom(typeof(TCommand));

    /// <inheritdoc />
    public Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_isControlPlane)
        {
            if (!tenantContext.HasTenant)
            {
                throw new TenantContextRequiredException(
                    $"Command '{typeof(TCommand).Name}' requires an active tenant context."
                );
            }

            principalValidator?.EnsurePrincipalMatchesTenant(tenantContext);
        }

        return inner.HandleAsync(command, cancellationToken);
    }
}
