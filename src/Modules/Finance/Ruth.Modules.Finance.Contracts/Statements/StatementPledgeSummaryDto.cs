namespace Ruth.Modules.Finance.Contracts.Statements;

/// <summary>Summary of a constituent or corporate pledge commitment and period fulfillment.</summary>
public sealed record StatementPledgeSummaryDto(
    Guid PledgeId,
    string AccountName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalCommittedAmount,
    decimal FulfilledAmount,
    decimal RemainingAmount,
    string CurrencyCode
);
