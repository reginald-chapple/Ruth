namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Input specification for an account allocation split on a recurring schedule.</summary>
/// <param name="AccountId">Target financial account identifier.</param>
/// <param name="Amount">Positive installment amount allocated to this account.</param>
/// <param name="Summary">Optional split memo.</param>
public sealed record RecurringScheduleSplitSpec(
    Guid AccountId,
    decimal Amount,
    string? Summary = null
);
