using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Ruth.Modules.Tenancy.Contracts.Hosts;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Seals the HTTP handshake's <see cref="TenantExecutionState" /> into an immutable <see cref="TenantCircuitState" />
/// when a Blazor Server circuit opens and enforces both tenant scope and principal <c lang="text">tid</c> claims across
/// reconnections and every inbound SignalR UI activity.
/// </summary>
public sealed class TenantCircuitHandler : CircuitHandler
{
    private readonly TenantCircuitState _circuitState;
    private readonly TenantWorkspaceBrandingState _brandingState;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;

    /// <summary>Initializes a new instance of the <see cref="TenantCircuitHandler" /> class.</summary>
    public TenantCircuitHandler(
        TenantCircuitState circuitState,
        TenantWorkspaceBrandingState brandingState,
        AuthenticationStateProvider? authenticationStateProvider = null
    )
    {
        ArgumentNullException.ThrowIfNull(circuitState);
        ArgumentNullException.ThrowIfNull(brandingState);

        _circuitState = circuitState;
        _brandingState = brandingState;
        _authenticationStateProvider = authenticationStateProvider;
        TryBindFromCurrentHandshake();
    }

    /// <inheritdoc />
    public override async Task OnCircuitOpenedAsync(
        Circuit circuit,
        CancellationToken cancellationToken
    )
    {
        if (!_circuitState.IsBound)
        {
            _circuitState.BindOrVerify(
                ResolveHandshakeHostKind(),
                AmbientTenantContext.CurrentState
            );
        }
        else
        {
            _circuitState.EnsureCompatibleAmbientState(AmbientTenantContext.CurrentState);
        }

        await RefreshAndValidatePrincipalAsync();
    }

    /// <inheritdoc />
    public override async Task OnConnectionUpAsync(
        Circuit circuit,
        CancellationToken cancellationToken
    )
    {
        _circuitState.EnsureCompatibleAmbientState(AmbientTenantContext.CurrentState);
        await RefreshAndValidatePrincipalAsync();
    }

    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        return async context =>
        {
            _circuitState.EnsureCompatibleAmbientState(AmbientTenantContext.CurrentState);
            await RefreshAndValidatePrincipalAsync();

            var priorTenant = AmbientTenantContext.CurrentState;
            AmbientTenantContext.SetCurrent(_circuitState.ExecutionState);
            using var principalScope = AmbientCircuitSecurityContext.BeginScope(
                _circuitState.Principal
            );
            try
            {
                await next(context);
            }
            finally
            {
                RestoreTenantState(priorTenant);
            }
        };
    }

    private void TryBindFromCurrentHandshake()
    {
        var current = AmbientTenantContext.CurrentState;
        var hostKind = ResolveHandshakeHostKind();
        if (
            (hostKind == ResolvedHostKind.TenantWorkspace && !current.TenantId.IsEmpty)
            || (hostKind == ResolvedHostKind.PlatformOperator && current.IsPlatformOperatorHost)
            || (
                hostKind == ResolvedHostKind.PlatformSignup
                && current == TenantExecutionState.Unresolved
            )
        )
        {
            _circuitState.BindOrVerify(hostKind, current);
        }
    }

    private ResolvedHostKind ResolveHandshakeHostKind()
    {
        var current = AmbientTenantContext.CurrentState;
        return current.IsPlatformOperatorHost ? ResolvedHostKind.PlatformOperator
            : !current.TenantId.IsEmpty && _brandingState.HostKind == ResolvedHostKind.Unknown
                ? ResolvedHostKind.TenantWorkspace
            : _brandingState.HostKind;
    }

    private async Task RefreshAndValidatePrincipalAsync()
    {
        if (_authenticationStateProvider is null)
        {
            _circuitState.ValidateAndCapturePrincipal(principal: null);
            return;
        }

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _circuitState.ValidateAndCapturePrincipal(authState.User);
    }

    private static void RestoreTenantState(TenantExecutionState prior)
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
