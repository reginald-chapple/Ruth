using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Queries the Revenue &amp; Impact Overview executive dashboard.</summary>
/// <param name="StartDateUtc">Optional start date (defaults to 6 months prior).</param>
/// <param name="EndDateUtc">Optional end date (defaults to now).</param>
/// <param name="BranchId">Optional branch filter.</param>
public sealed record GetRevenueAndImpactOverviewQuery(
    DateTimeOffset? StartDateUtc = null,
    DateTimeOffset? EndDateUtc = null,
    Guid? BranchId = null
) : IQuery<RevenueAndImpactOverviewResponse>;
