using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to pause an active recurring schedule.</summary>
/// <param name="ScheduleId">Target recurring schedule identifier.</param>
public sealed record PauseRecurringScheduleCommand(Guid ScheduleId)
    : ICommand<RecurringScheduleResponse>;
