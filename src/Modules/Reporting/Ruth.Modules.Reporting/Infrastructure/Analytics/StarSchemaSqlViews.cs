using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Reporting.Infrastructure.Analytics;

internal static class StarSchemaSqlViews
{
    public static readonly string[] ViewNames =
    [
        "reporting.vw_bi_metric_facts",
        "reporting.vw_bi_revenue_summary",
        "reporting.vw_bi_attendance_summary",
    ];

    public static async Task EnsureBiViewsAsync(ReportingDbContext db, CancellationToken ct)
    {
        const string metricViewSql =
            "CREATE OR REPLACE VIEW reporting.vw_bi_metric_facts AS "
            + "SELECT f.\"DateKey\" AS date_key, d.\"CalendarDate\" AS calendar_date, d.\"Year\" AS year, "
            + "d.\"Quarter\" AS quarter, d.\"MonthLabel\" AS month_label, "
            + "m.\"MetricKey\" AS metric_key, m.\"Title\" AS metric_title, m.\"UnitLabel\" AS unit_label, "
            + "f.\"PartitionEntityId\" AS partition_entity_id, f.\"PartitionLabel\" AS partition_label, "
            + "f.\"MeasureSum\" AS measure_sum, f.\"GoalSum\" AS goal_sum, "
            + "f.\"SampleCount\" AS sample_count, f.\"RefreshedAtUtc\" AS refreshed_at_utc "
            + "FROM reporting.fact_metric_daily f "
            + "LEFT JOIN reporting.dim_date d ON d.\"DateKey\" = f.\"DateKey\" "
            + "LEFT JOIN reporting.dim_metric m ON m.\"MetricId\" = f.\"MetricId\";";

        const string revenueViewSql =
            "CREATE OR REPLACE VIEW reporting.vw_bi_revenue_summary AS "
            + "SELECT r.\"MonthDateKey\" AS month_date_key, r.\"MonthLabel\" AS month_label, r.\"BranchId\" AS branch_id, "
            + "COALESCE(b.\"BranchName\", 'Organization Wide') AS branch_name, "
            + "r.\"TaxDeductibleGiving\" AS tax_deductible_giving, "
            + "r.\"NonDeductibleDuesAndFees\" AS non_deductible_dues_and_fees, "
            + "(r.\"TaxDeductibleGiving\" + r.\"NonDeductibleDuesAndFees\") AS total_revenue, "
            + "r.\"TransactionCount\" AS transaction_count, r.\"RefreshedAtUtc\" AS refreshed_at_utc "
            + "FROM reporting.fact_revenue_monthly r "
            + "LEFT JOIN reporting.dim_branch b ON b.\"BranchId\" = r.\"BranchId\";";

        const string attendanceViewSql =
            "CREATE OR REPLACE VIEW reporting.vw_bi_attendance_summary AS "
            + "SELECT a.\"WeekEndDateKey\" AS week_end_date_key, a.\"WeekEndDate\" AS week_end_date, a.\"BranchId\" AS branch_id, "
            + "COALESCE(b.\"BranchName\", 'Organization Wide') AS branch_name, "
            + "a.\"OccurrenceCount\" AS occurrence_count, a.\"AttendedCount\" AS attended_count, "
            + "a.\"RefreshedAtUtc\" AS refreshed_at_utc "
            + "FROM reporting.fact_attendance_weekly a "
            + "LEFT JOIN reporting.dim_branch b ON b.\"BranchId\" = a.\"BranchId\";";

        _ = await db.Database.ExecuteSqlRawAsync(metricViewSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(revenueViewSql, ct);
        _ = await db.Database.ExecuteSqlRawAsync(attendanceViewSql, ct);
    }
}
