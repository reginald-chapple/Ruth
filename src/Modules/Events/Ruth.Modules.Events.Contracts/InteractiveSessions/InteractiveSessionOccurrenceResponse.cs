namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Live run occurrence of an InteractiveSession (e.g., per town-hall or conference room session).</summary>
public sealed record InteractiveSessionOccurrenceResponse(
    Guid Id,
    Guid SessionId,
    Guid? EventItemOccurrenceId,
    Guid? BranchId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    int ParticipantResponseCount
);
