namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Aggregated commitment and fulfillment analytics across financial pledges.</summary>
/// <param name="CurrencyCode">Currency code of the aggregated pledges.</param>
/// <param name="ActivePledgeCount">Number of active pledges matching the query.</param>
/// <param name="CompletedPledgeCount">Number of completed pledges matching the query.</param>
/// <param name="TotalPledgedAmount">Sum of total committed amounts across active and completed pledges.</param>
/// <param name="TotalFulfilledAmount">Sum of settled contributions matched to pledges.</param>
/// <param name="TotalRemainingAmount">Sum of remaining balances across active pledges.</param>
/// <param name="OverallFulfillmentPercent">Overall fulfillment rate percentage.</param>
/// <param name="OnTrackPledgeCount">Count of active pledges meeting or exceeding pro-rata pace.</param>
/// <param name="BehindPacePledgeCount">Count of active pledges trailing pro-rata expected pace.</param>
public sealed record PledgeAnalyticsSummaryResponse(
    string CurrencyCode,
    int ActivePledgeCount,
    int CompletedPledgeCount,
    decimal TotalPledgedAmount,
    decimal TotalFulfilledAmount,
    decimal TotalRemainingAmount,
    decimal OverallFulfillmentPercent,
    int OnTrackPledgeCount,
    int BehindPacePledgeCount
);
