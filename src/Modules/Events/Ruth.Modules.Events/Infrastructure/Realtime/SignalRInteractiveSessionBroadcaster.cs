using Microsoft.AspNetCore.SignalR;
using Ruth.Modules.Events.Contracts.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Realtime;

internal sealed class SignalRInteractiveSessionBroadcaster(
    IHubContext<InteractiveSessionHub> hubContext
) : IInteractiveSessionBroadcaster
{
    public async Task BroadcastStageProjectionAsync(
        InteractiveStageProjectionResponse projection,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(projection);
        await hubContext
            .Clients.Group(InteractiveSessionHub.GetSessionGroupName(projection.SessionId))
            .SendAsync(
                InteractiveSessionHub.StageProjectionUpdatedMethod,
                projection,
                cancellationToken
            );
    }
}
