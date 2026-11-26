using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Command for self-service or staff updates to a recurring schedule's payment method, cadence, or splits.</summary>
/// <param name="ScheduleId">Target recurring schedule identifier.</param>
/// <param name="SavedPaymentMethodId">Updated vaulted payment method identifier.</param>
/// <param name="Frequency">Updated installment cadence.</param>
/// <param name="NextPaymentDate">Updated next billing date.</param>
/// <param name="Splits">Updated positive account splits.</param>
/// <param name="EndDate">Optional updated end date.</param>
public sealed record UpdateRecurringScheduleCommand(
    Guid ScheduleId,
    Guid SavedPaymentMethodId,
    PledgeFrequency Frequency,
    DateOnly NextPaymentDate,
    IReadOnlyList<RecurringScheduleSplitSpec> Splits,
    DateOnly? EndDate = null
) : ICommand<RecurringScheduleResponse>;
