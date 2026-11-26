namespace Ruth.Modules.Events.Contracts.InteractiveSessions;

/// <summary>Real-time option vote tally for a multiple-choice interactive poll.</summary>
public sealed record InteractivePollOptionTallyDto(
    string OptionLabel,
    int VoteCount,
    decimal VotePercent
);
