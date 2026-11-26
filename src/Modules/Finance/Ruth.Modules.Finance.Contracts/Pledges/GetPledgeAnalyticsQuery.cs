using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Query to compute commitment and fulfillment analytics for pledges.</summary>
/// <param name="AccountId">Optional filter by financial account identifier.</param>
/// <param name="BranchId">Optional filter by Branch/Site identifier.</param>
/// <param name="CurrencyCode">Currency code to aggregate (defaults to USD).</param>
/// <param name="AsOfDate">Optional evaluation date for pace comparison.</param>
public sealed record GetPledgeAnalyticsQuery(
    Guid? AccountId = null,
    Guid? BranchId = null,
    string CurrencyCode = "USD",
    DateOnly? AsOfDate = null
) : IQuery<PledgeAnalyticsSummaryResponse>;
