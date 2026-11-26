namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Outcome of compiling and routing all badge and receipt labels for a check-in session.</summary>
/// <param name="SessionId">Check-in session identifier.</param>
/// <param name="ServerDispatchedCount">Number of jobs sent directly via server TCP raw socket.</param>
/// <param name="ClientPendingCount">Number of jobs returned for client WebUSB or browser local printing.</param>
/// <param name="Jobs">Compiled label print jobs.</param>
public sealed record PrintCheckInSessionLabelsResponse(
    Guid SessionId,
    int ServerDispatchedCount,
    int ClientPendingCount,
    IReadOnlyList<PrintedLabelJobDto> Jobs
);
