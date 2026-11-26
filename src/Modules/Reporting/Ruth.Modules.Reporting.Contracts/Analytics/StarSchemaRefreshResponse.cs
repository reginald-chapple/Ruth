namespace Ruth.Modules.Reporting.Contracts.Analytics;

/// <summary>Result of refreshing materialized PostgreSQL star-schema tables and BI views.</summary>
/// <param name="RefreshedAtUtc">UTC timestamp of refresh completion.</param>
/// <param name="DimDateRows">Rows in reporting.dim_date.</param>
/// <param name="DimBranchRows">Rows in reporting.dim_branch.</param>
/// <param name="DimMetricRows">Rows in reporting.dim_metric.</param>
/// <param name="FactMetricDailyRows">Rows in reporting.fact_metric_daily.</param>
/// <param name="FactRevenueMonthlyRows">Rows in reporting.fact_revenue_monthly.</param>
/// <param name="FactAttendanceWeeklyRows">Rows in reporting.fact_attendance_weekly.</param>
/// <param name="AvailableBiViews">PostgreSQL BI view names ready for external reporting tools.</param>
public sealed record StarSchemaRefreshResponse(
    DateTimeOffset RefreshedAtUtc,
    int DimDateRows,
    int DimBranchRows,
    int DimMetricRows,
    int FactMetricDailyRows,
    int FactRevenueMonthlyRows,
    int FactAttendanceWeeklyRows,
    IReadOnlyList<string> AvailableBiViews
);
