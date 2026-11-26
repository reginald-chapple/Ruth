using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command to project a specific action onto the live stage via SignalR.</summary>
public sealed record SetActiveInteractiveActionCommand(Guid SessionId, Guid? ActionId)
    : ICommand<InteractiveStageProjectionResponse>;
