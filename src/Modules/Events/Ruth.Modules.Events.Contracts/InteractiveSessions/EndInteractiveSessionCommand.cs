using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Command to conclude a live interactive session.</summary>
public sealed record EndInteractiveSessionCommand(Guid SessionId)
    : ICommand<InteractiveSessionDetailResponse>;
