namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Represents a constituent's recorded milestone step in a JourneyProgram.</summary>
/// <param name="Id">PersonMilestone identifier.</param>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="JourneyProgramId">JourneyProgram identifier.</param>
/// <param name="MilestoneTypeId">MilestoneType identifier.</param>
/// <param name="MilestoneName">Milestone display name.</param>
/// <param name="MilestoneStatusId">MilestoneStatus identifier.</param>
/// <param name="StatusName">MilestoneStatus display name.</param>
/// <param name="IsCompleted">Whether the status marks completion.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="StartDate">Start date.</param>
/// <param name="CompletedDate">Optional completion date.</param>
/// <param name="EndDate">Optional end date.</param>
/// <param name="Note">Optional note.</param>
public sealed record PersonMilestoneResponse(
    Guid Id,
    Guid PersonId,
    Guid JourneyProgramId,
    Guid MilestoneTypeId,
    string MilestoneName,
    Guid MilestoneStatusId,
    string StatusName,
    bool IsCompleted,
    Guid? BranchId,
    DateOnly StartDate,
    DateOnly? CompletedDate,
    DateOnly? EndDate,
    string? Note
);
