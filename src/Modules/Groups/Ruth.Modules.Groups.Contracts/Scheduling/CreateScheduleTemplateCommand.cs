using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Creates a GroupMemberScheduleTemplate.</summary>
public sealed record CreateScheduleTemplateCommand(
    string Name,
    Guid ScheduleId,
    Guid? GroupTypeId = null,
    string RecurrenceDescription = "Every Week",
    int IntervalWeeks = 1
) : ICommand<GroupMemberScheduleTemplateResponse>;
