using Microsoft.AspNetCore.SignalR;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Seals the SignalR connection handshake's <see cref="TenantExecutionState" /> and restores + validates it
/// across every hub method invocation.
/// </summary>
internal sealed class TenantSignalRHubFilter : IHubFilter
{
    public const string ConnectionTenantStateItemKey = "Stealth.SignalR.TenantExecutionState";

    public async Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var handshakeState = AmbientTenantContext.CurrentState;
        if (handshakeState == TenantExecutionState.Unresolved)
        {
            throw new TenantContextRequiredException(
                "SignalR hub connections require a resolved tenant or operator workspace."
            );
        }

        EnsureSameOriginWebSocketRequest(context.Context.GetHttpContext());

        var hostKind = handshakeState.IsPlatformOperatorHost
            ? ResolvedHostKind.PlatformOperator
            : ResolvedHostKind.TenantWorkspace;
        TenantCircuitState.ValidatePrincipalAgainstExecutionState(
            context.Context.User,
            hostKind,
            handshakeState
        );

        context.Context.Items[ConnectionTenantStateItemKey] = handshakeState;
        using var principalScope = AmbientCircuitSecurityContext.BeginScope(context.Context.User);
        await next(context);
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next
    )
    {
        ArgumentNullException.ThrowIfNull(invocationContext);
        ArgumentNullException.ThrowIfNull(next);

        if (
            !invocationContext.Context.Items.TryGetValue(ConnectionTenantStateItemKey, out var raw)
            || raw is not TenantExecutionState boundState
            || boundState == TenantExecutionState.Unresolved
        )
        {
            throw new TenantContextRequiredException(
                "SignalR hub invocation cannot execute without a bound handshake tenant state."
            );
        }

        var ambient = AmbientTenantContext.CurrentState;
        if (
            ambient != TenantExecutionState.Unresolved
            && (
                ambient.TenantId != boundState.TenantId
                || ambient.IsPlatformOperatorHost != boundState.IsPlatformOperatorHost
            )
        )
        {
            throw new CrossTenantAccessException(
                "SignalR hub invocation attempted to cross tenant workspace boundaries."
            );
        }

        var hostKind = boundState.IsPlatformOperatorHost
            ? ResolvedHostKind.PlatformOperator
            : ResolvedHostKind.TenantWorkspace;
        TenantCircuitState.ValidatePrincipalAgainstExecutionState(
            invocationContext.Context.User,
            hostKind,
            boundState
        );

        var prior = AmbientTenantContext.CurrentState;
        AmbientTenantContext.SetCurrent(boundState);
        using var principalScope = AmbientCircuitSecurityContext.BeginScope(
            invocationContext.Context.User
        );
        try
        {
            return await next(invocationContext);
        }
        finally
        {
            if (prior == TenantExecutionState.Unresolved)
            {
                AmbientTenantContext.ClearCurrent();
            }
            else
            {
                AmbientTenantContext.SetCurrent(prior);
            }
        }
    }

    private static void EnsureSameOriginWebSocketRequest(HttpContext? httpContext)
    {
        if (
            httpContext is null
            || !httpContext.Request.Headers.TryGetValue("Origin", out var originValues)
        )
        {
            return;
        }

        var rawOrigin = originValues.ToString().Trim();
        if (string.IsNullOrEmpty(rawOrigin))
        {
            return;
        }

        if (
            !Uri.TryCreate(rawOrigin, UriKind.Absolute, out var originUri)
            || !string.Equals(
                originUri.Host,
                httpContext.Request.Host.Host,
                StringComparison.OrdinalIgnoreCase
            )
            || (
                httpContext.Request.Host.Port.HasValue
                && !originUri.IsDefaultPort
                && originUri.Port != httpContext.Request.Host.Port.Value
            )
        )
        {
            throw new HubException("Cross-origin SignalR/WebSocket connections are forbidden.");
        }
    }
}
