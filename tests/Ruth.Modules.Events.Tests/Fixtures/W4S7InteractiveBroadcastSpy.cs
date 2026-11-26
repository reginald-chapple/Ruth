using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure.Realtime;

namespace Ruth.Modules.Events.Tests.Fixtures;

internal sealed class W4S7InteractiveBroadcastSpy : IInteractiveSessionBroadcaster
{
    private readonly List<InteractiveStageProjectionResponse> _projections = [];

    public IReadOnlyList<InteractiveStageProjectionResponse> Projections => _projections;

    public Task BroadcastStageProjectionAsync(
        InteractiveStageProjectionResponse projection,
        CancellationToken cancellationToken = default
    )
    {
        _projections.Add(projection);
        return Task.CompletedTask;
    }
}
