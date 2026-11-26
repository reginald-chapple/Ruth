namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Represents a constituent's full completion of a JourneyProgram.</summary>
/// <param name="Id">JourneyCompletion identifier.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="JourneyProgramId">JourneyProgram identifier.</param>
/// <param name="JourneyProgramName">JourneyProgram display name.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="StartedDate">Date of earliest milestone in program.</param>
/// <param name="CompletedDate">Date program completion criteria were satisfied.</param>
public sealed record JourneyCompletionResponse(
    Guid Id,
    Guid PersonId,
    Guid JourneyProgramId,
    string JourneyProgramName,
    Guid? BranchId,
    DateOnly StartedDate,
    DateOnly CompletedDate
);
