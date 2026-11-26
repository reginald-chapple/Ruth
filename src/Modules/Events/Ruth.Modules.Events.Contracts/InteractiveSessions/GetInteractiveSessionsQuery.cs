using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Query to list interactive sessions for moderators.</summary>
public sealed record GetInteractiveSessionsQuery(InteractiveSessionStatus? Status = null)
    : IQuery<IReadOnlyList<InteractiveSessionDetailResponse>>;
