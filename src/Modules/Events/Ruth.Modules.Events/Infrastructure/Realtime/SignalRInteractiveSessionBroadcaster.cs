using Microsoft.AspNetCore.SignalR;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events.Infrastructure.Realtime;

internal sealed class SignalRInteractiveSessionBroadcaster(
    IHubContext<InteractiveSessionHub> hubContext,
    ITenantContext tenantContext
) : IInteractiveSessionBroadcaster
{
    public async Task BroadcastStageProjectionAsync(
        InteractiveStageProjectionResponse projection,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        var tenantId = tenantContext.TenantId;
        if (tenantId.IsEmpty)
        {
            return;
        }

        await hubContext
            .Clients.Group(
                InteractiveSessionHub.GetSessionGroupName(tenantId, projection.SessionId)
            )
            .SendAsync(
                InteractiveSessionHub.StageProjectionUpdatedMethod,
                projection,
                cancellationToken
            );
    }
}
