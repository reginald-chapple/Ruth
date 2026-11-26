using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Query to retrieve live stage projection and active prompt state by session GUID or 6-character join code.</summary>
public sealed record GetInteractiveSessionStageQuery(string SessionIdOrJoinCode)
    : IQuery<InteractiveStageProjectionResponse>;
