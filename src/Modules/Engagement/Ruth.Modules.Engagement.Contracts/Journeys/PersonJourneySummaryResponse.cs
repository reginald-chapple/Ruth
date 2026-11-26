namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Aggregated Journey progress summary for a constituent across a JourneyProgram.</summary>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="JourneyProgramId">JourneyProgram identifier.</param>
/// <param name="ProgramName">JourneyProgram display name.</param>
/// <param name="CompletedRequiredCount">Count of completed required milestones.</param>
/// <param name="TotalRequiredCount">Total count of required milestones.</param>
/// <param name="IsProgramCompleted">Whether the program has been completed.</param>
/// <param name="ProgramCompletedDate">Optional completion date.</param>
/// <param name="Milestones">Recorded milestones for this person.</param>
public sealed record PersonJourneySummaryResponse(
    Guid PersonId,
    Guid JourneyProgramId,
    string ProgramName,
    int CompletedRequiredCount,
    int TotalRequiredCount,
    bool IsProgramCompleted,
    DateOnly? ProgramCompletedDate,
    IReadOnlyList<PersonMilestoneResponse> Milestones
);
