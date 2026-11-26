namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Read projection for an account split on a recurring schedule.</summary>
/// <param name="Id">Split identifier.</param>
/// <param name="AccountId">Financial account identifier.</param>
/// <param name="AccountName">Financial account display name.</param>
/// <param name="Amount">Installment split amount.</param>
/// <param name="Summary">Optional split memo.</param>
public sealed record RecurringScheduleSplitResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    decimal Amount,
    string? Summary
);
