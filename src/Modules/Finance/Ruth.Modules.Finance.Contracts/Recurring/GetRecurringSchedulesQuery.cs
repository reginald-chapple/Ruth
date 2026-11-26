using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Query to list recurring contribution and membership subscription schedules.</summary>
/// <param name="PersonId">Optional filter by constituent or organization Person identifier.</param>
/// <param name="Status">Optional filter by recurring schedule status.</param>
/// <param name="Purpose">Optional filter by schedule purpose.</param>
public sealed record GetRecurringSchedulesQuery(
    Guid? PersonId = null,
    RecurringScheduleStatus? Status = null,
    RecurringSchedulePurpose? Purpose = null
) : IQuery<IReadOnlyList<RecurringScheduleResponse>>;
