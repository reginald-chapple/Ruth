using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Auto-populates open serving slots up to DesiredCapacity using standing assignments and templates.</summary>
public sealed record AutoPopulateServingScheduleCommand(
    Guid GroupId,
    DateOnly OccurrenceDate,
    Guid? ScheduledByPersonId = null
) : ICommand<AutoPopulateScheduleResponse>;
