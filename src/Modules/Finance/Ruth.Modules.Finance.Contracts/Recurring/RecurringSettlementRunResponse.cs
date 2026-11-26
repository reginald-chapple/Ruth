namespace Ruth.Modules.Finance.Contracts.Recurring;

/// <summary>Summary returned after executing a recurring billing settlement sync batch.</summary>
/// <param name="EvaluatedCount">Number of due recurring schedules evaluated.</param>
/// <param name="SettledCount">Number of schedules charged and settled successfully.</param>
/// <param name="FailedCount">Number of schedules whose gateway charge failed.</param>
/// <param name="TotalSettledAmount">Sum of settled recurring transaction amounts.</param>
public sealed record RecurringSettlementRunResponse(
    int EvaluatedCount,
    int SettledCount,
    int FailedCount,
    decimal TotalSettledAmount
);
