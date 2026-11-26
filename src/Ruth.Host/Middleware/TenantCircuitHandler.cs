using Microsoft.AspNetCore.Components.Server.Circuits;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Middleware;

/// <summary>
/// Captures the HTTP handshake's <see cref="TenantExecutionState" /> when a Blazor Server circuit opens
/// and restores it across every inbound SignalR activity so interactive UI events never lose or cross tenant scope.
/// </summary>
internal sealed class TenantCircuitHandler : CircuitHandler
{
    private TenantExecutionState _capturedState = AmbientTenantContext.CurrentState;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var current = AmbientTenantContext.CurrentState;
        if (current != TenantExecutionState.Unresolved)
        {
            _capturedState = current;
        }

        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        return async context =>
        {
            var state = _capturedState;
            var prior = AmbientTenantContext.CurrentState;
            AmbientTenantContext.SetCurrent(state);
            try
            {
                await next(context);
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
        };
    }
}
