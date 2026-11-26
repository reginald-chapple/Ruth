using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to permanently cancel a recurring schedule.</summary>
/// <param name="ScheduleId">Target recurring schedule identifier.</param>
/// <param name="Reason">Optional cancellation reason.</param>
public sealed record CancelRecurringScheduleCommand(Guid ScheduleId, string? Reason = null)
    : ICommand<RecurringScheduleResponse>;
