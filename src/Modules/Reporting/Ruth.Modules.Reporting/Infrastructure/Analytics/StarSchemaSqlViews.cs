using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Reporting.Infrastructure.Analytics;

internal static class StarSchemaSqlViews
{
    public static readonly string[] ViewNames =
    [
        "reporting.vw_bi_metric_facts",
        "reporting.vw_bi_revenue_summary",
        "reporting.vw_bi_attendance_summary",
    ];

    private const string MetricViewSql =
        "CREATE OR REPLACE VIEW reporting.vw_bi_metric_facts WITH (security_invoker = true) AS "
        + "SELECT f.\"DateKey\" AS date_key, d.\"CalendarDate\" AS calendar_date, d.\"Year\" AS year, "
        + "d.\"Quarter\" AS quarter, d.\"MonthLabel\" AS month_label, "
        + "m.\"MetricKey\" AS metric_key, m.\"Title\" AS metric_title, m.\"UnitLabel\" AS unit_label, "
        + "f.\"PartitionEntityId\" AS partition_entity_id, f.\"PartitionLabel\" AS partition_label, "
        + "f.\"MeasureSum\" AS measure_sum, f.\"GoalSum\" AS goal_sum, "
        + "f.\"SampleCount\" AS sample_count, f.\"RefreshedAtUtc\" AS refreshed_at_utc, "
        + "f.tenant_id AS tenant_id "
        + "FROM reporting.fact_metric_daily f "
        + "LEFT JOIN reporting.dim_date d ON d.tenant_id = f.tenant_id AND d.\"DateKey\" = f.\"DateKey\" "
        + "LEFT JOIN reporting.dim_metric m ON m.tenant_id = f.tenant_id AND m.\"MetricId\" = f.\"MetricId\" "
        + "WHERE f.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid;";

    private const string RevenueViewSql =
        "CREATE OR REPLACE VIEW reporting.vw_bi_revenue_summary WITH (security_invoker = true) AS "
        + "SELECT r.\"MonthDateKey\" AS month_date_key, r.\"MonthLabel\" AS month_label, r.\"BranchId\" AS branch_id, "
        + "COALESCE(b.\"BranchName\", 'Organization Wide') AS branch_name, "
        + "r.\"TaxDeductibleGiving\" AS tax_deductible_giving, "
        + "r.\"NonDeductibleDuesAndFees\" AS non_deductible_dues_and_fees, "
        + "(r.\"TaxDeductibleGiving\" + r.\"NonDeductibleDuesAndFees\") AS total_revenue, "
        + "r.\"TransactionCount\" AS transaction_count, r.\"RefreshedAtUtc\" AS refreshed_at_utc, "
        + "r.tenant_id AS tenant_id "
        + "FROM reporting.fact_revenue_monthly r "
        + "LEFT JOIN reporting.dim_branch b ON b.tenant_id = r.tenant_id AND b.\"BranchId\" = r.\"BranchId\" "
        + "WHERE r.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid;";

    private const string AttendanceViewSql =
        "CREATE OR REPLACE VIEW reporting.vw_bi_attendance_summary WITH (security_invoker = true) AS "
        + "SELECT a.\"WeekEndDateKey\" AS week_end_date_key, a.\"WeekEndDate\" AS week_end_date, a.\"BranchId\" AS branch_id, "
        + "COALESCE(b.\"BranchName\", 'Organization Wide') AS branch_name, "
        + "a.\"OccurrenceCount\" AS occurrence_count, a.\"AttendedCount\" AS attended_count, "
        + "a.\"RefreshedAtUtc\" AS refreshed_at_utc, "
        + "a.tenant_id AS tenant_id "
        + "FROM reporting.fact_attendance_weekly a "
        + "LEFT JOIN reporting.dim_branch b ON b.tenant_id = a.tenant_id AND b.\"BranchId\" = a.\"BranchId\" "
        + "WHERE a.tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid;";

    public static async Task EnsureBiViewsAsync(
        PostgresRlsRoleProvisioner? provisioner,
        ReportingDbContext? fallbackDb,
        CancellationToken ct
    )
    {
        if (provisioner is not null)
        {
            await using var conn = new NpgsqlConnection(provisioner.AdminConnectionString);
            await conn.OpenAsync(ct);
            foreach (var sql in new[] { MetricViewSql, RevenueViewSql, AttendanceViewSql })
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                _ = await cmd.ExecuteNonQueryAsync(ct);
            }

            await provisioner.EnsureAppRoleAndSchemaGrantsAsync("reporting", ct);
            return;
        }

        if (fallbackDb is not null)
        {
            _ = await fallbackDb.Database.ExecuteSqlRawAsync(MetricViewSql, ct);
            _ = await fallbackDb.Database.ExecuteSqlRawAsync(RevenueViewSql, ct);
            _ = await fallbackDb.Database.ExecuteSqlRawAsync(AttendanceViewSql, ct);
        }
    }
}
