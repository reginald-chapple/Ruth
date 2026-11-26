using Ruth.Modules.Events.Contracts.InteractiveSessions;

namespace Ruth.Modules.Events.Infrastructure.Realtime;

internal interface IInteractiveSessionBroadcaster
{
    Task BroadcastStageProjectionAsync(
        InteractiveStageProjectionResponse projection,
        CancellationToken cancellationToken = default
    );
}
