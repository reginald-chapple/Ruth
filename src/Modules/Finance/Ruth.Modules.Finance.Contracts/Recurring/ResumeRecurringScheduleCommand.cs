using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command to resume a paused or past-due recurring schedule.</summary>
/// <param name="ScheduleId">Target recurring schedule identifier.</param>
/// <param name="NextPaymentDate">Optional explicit next billing date.</param>
public sealed record ResumeRecurringScheduleCommand(
    Guid ScheduleId,
    DateOnly? NextPaymentDate = null
) : ICommand<RecurringScheduleResponse>;
