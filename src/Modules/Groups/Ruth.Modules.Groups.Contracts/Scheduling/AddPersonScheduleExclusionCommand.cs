using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Adds a personal (or family-linked) blackout date range for a Person.</summary>
public sealed record AddPersonScheduleExclusionCommand(
    Guid PersonId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Title = null,
    Guid? GroupId = null,
    Guid? ParentExclusionId = null
) : ICommand<PersonScheduleExclusionResponse>;
