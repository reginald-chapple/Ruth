using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Queries the Executive Insights dashboard KPIs.</summary>
/// <param name="WindowDays">Lookback period in days (default 30).</param>
/// <param name="BranchId">Optional branch filter.</param>
public sealed record GetExecutiveInsightsQuery(int WindowDays = 30, Guid? BranchId = null)
    : IQuery<ExecutiveInsightsResponse>;
