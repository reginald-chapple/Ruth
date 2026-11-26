using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command to start a live occurrence of an interactive session and broadcast to SignalR stage/audience clients.</summary>
public sealed record StartInteractiveSessionOccurrenceCommand(
    Guid SessionId,
    Guid? EventItemOccurrenceId = null,
    Guid? BranchId = null
) : ICommand<InteractiveSessionDetailResponse>;
