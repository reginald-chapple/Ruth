using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Adds a GroupType-level schedule exclusion blackout date range.</summary>
public sealed record AddGroupScheduleExclusionCommand(
    Guid GroupTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Title = null
) : ICommand<GroupScheduleExclusionResponse>;
