using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Journeys;

/// <summary>Command to record or complete a PersonMilestone within a JourneyProgram.</summary>
/// <param name="PersonId">Constituent Person identifier.</param>
/// <param name="JourneyProgramId">JourneyProgram identifier.</param>
/// <param name="MilestoneTypeId">Target MilestoneType identifier.</param>
/// <param name="MilestoneStatusId">Target MilestoneStatus identifier.</param>
/// <param name="StartDate">Milestone start date.</param>
/// <param name="CompletedDate">Optional completion date (defaults to StartDate when status IsCompleteStatus).</param>
/// <param name="EndDate">Optional end date.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="Note">Optional note.</param>
public sealed record RecordPersonMilestoneCommand(
    Guid PersonId,
    Guid JourneyProgramId,
    Guid MilestoneTypeId,
    Guid MilestoneStatusId,
    DateOnly StartDate,
    DateOnly? CompletedDate = null,
    DateOnly? EndDate = null,
    Guid? BranchId = null,
    string? Note = null
) : ICommand<PersonMilestoneResponse>;
