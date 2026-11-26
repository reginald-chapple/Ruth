namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Real-time stage projection state pushed via SignalR to town-hall, classroom, and conference displays.</summary>
public sealed record InteractiveStageProjectionResponse(
    Guid SessionId,
    string Title,
    string JoinCode,
    InteractiveSessionStatus Status,
    Guid? ActiveOccurrenceId,
    InteractiveActionResponse? ActiveAction,
    int TotalResponsesForActiveAction,
    IReadOnlyList<InteractivePollOptionTallyDto> PollTallies,
    IReadOnlyList<InteractiveResponseDto> ApprovedAudienceMessages
);
