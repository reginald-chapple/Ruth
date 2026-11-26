using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Domain.Analytics;
using Ruth.Modules.Reporting.Infrastructure.DataViews;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Reporting.Infrastructure.Analytics;

internal sealed class StarSchemaProjectionRefresher(
    ReportingDbContext db,
    ReportingDataViewExecutionEngine sourceReader,
    IClock clock,
    PostgresRlsRoleProvisioner? provisioner = null
)
{
    public async Task<StarSchemaRefreshResponse> RefreshAsync(CancellationToken ct)
    {
        var nowUtc = clock.UtcNow;
        await EnsureDimDatesAsync(nowUtc, ct);
        var branchCount = await RefreshDimBranchesAsync(nowUtc, ct);
        var metricDimCount = await RefreshDimMetricsAsync(nowUtc, ct);
        var metricFactCount = await RefreshFactMetricDailyAsync(nowUtc, ct);
        var revenueFactCount = await RefreshFactRevenueMonthlyAsync(nowUtc, ct);
        var attendanceFactCount = await RefreshFactAttendanceWeeklyAsync(nowUtc, ct);
        await StarSchemaSqlViews.EnsureBiViewsAsync(provisioner, db, ct);

        return new StarSchemaRefreshResponse(
            nowUtc,
            await db.DimDates.CountAsync(ct),
            branchCount,
            metricDimCount,
            metricFactCount,
            revenueFactCount,
            attendanceFactCount,
            StarSchemaSqlViews.ViewNames
        );
    }

    public async Task<StarSchemaRefreshResponse> GetStatusAsync(CancellationToken ct)
    {
        var lastRefreshed = await db
            .FactMetricsDaily.AsNoTracking()
            .Select(static f => (DateTimeOffset?)f.RefreshedAtUtc)
            .MaxAsync(ct)
            ?? await db
                .DimMetrics.AsNoTracking()
                .Select(static m => (DateTimeOffset?)m.RefreshedAtUtc)
                .MaxAsync(ct)
            ?? clock.UtcNow;

        return new StarSchemaRefreshResponse(
            lastRefreshed,
            await db.DimDates.CountAsync(ct),
            await db.DimBranches.CountAsync(ct),
            await db.DimMetrics.CountAsync(ct),
            await db.FactMetricsDaily.CountAsync(ct),
            await db.FactRevenuesMonthly.CountAsync(ct),
            await db.FactAttendancesWeekly.CountAsync(ct),
            StarSchemaSqlViews.ViewNames
        );
    }

    private async Task EnsureDimDatesAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        var start = DateOnly.FromDateTime(nowUtc.UtcDateTime).AddDays(-365);
        var end = DateOnly.FromDateTime(nowUtc.UtcDateTime).AddDays(90);
        var existingSet = (
            await db.DimDates.AsNoTracking().Select(static d => d.DateKey).ToListAsync(ct)
        ).ToHashSet();

        for (var cursor = start; cursor <= end; cursor = cursor.AddDays(1))
        {
            var key = ToDateKey(cursor);
            if (existingSet.Contains(key))
            {
                continue;
            }

            _ = db.DimDates.Add(
                new DimDate
                {
                    DateKey = key,
                    CalendarDate = cursor,
                    Year = cursor.Year,
                    Quarter = ((cursor.Month - 1) / 3) + 1,
                    Month = cursor.Month,
                    MonthLabel = cursor.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    IsoWeek = ISOWeek.GetWeekOfYear(cursor.ToDateTime(TimeOnly.MinValue)),
                    DayOfWeek = (int)cursor.DayOfWeek,
                }
            );
        }

        _ = await db.SaveChangesAsync(ct);
    }

    private async Task<int> RefreshDimBranchesAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (!await sourceReader.TableExistsAsync("core.branches", ct))
        {
            return await db.DimBranches.CountAsync(ct);
        }

        _ = await db.DimBranches.ExecuteDeleteAsync(ct);
        await ReadRowsAsync(
            "SELECT id, name, short_code, is_active FROM core.branches",
            async reader =>
            {
                _ = db.DimBranches.Add(
                    new DimBranch
                    {
                        BranchId = reader.GetGuid(0),
                        BranchName = reader.GetString(1),
                        ShortCode = await reader.IsDBNullAsync(2, ct) ? null : reader.GetString(2),
                        IsActive = !await reader.IsDBNullAsync(3, ct) && reader.GetBoolean(3),
                        RefreshedAtUtc = nowUtc,
                    }
                );
            },
            ct
        );
        _ = await db.SaveChangesAsync(ct);
        return await db.DimBranches.CountAsync(ct);
    }

    private async Task<int> RefreshDimMetricsAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        _ = await db.DimMetrics.ExecuteDeleteAsync(ct);
        var metrics = await db.Metrics.AsNoTracking().ToListAsync(ct);
        foreach (var m in metrics)
        {
            _ = db.DimMetrics.Add(
                new DimMetric
                {
                    MetricId = m.Id.Value,
                    MetricKey = m.MetricKey,
                    Title = m.Title,
                    UnitLabel = m.UnitLabel,
                    SourceType = m.SourceType.ToString(),
                    RefreshedAtUtc = nowUtc,
                }
            );
        }

        _ = await db.SaveChangesAsync(ct);
        return metrics.Count;
    }

    private async Task<int> RefreshFactMetricDailyAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        _ = await db.FactMetricsDaily.ExecuteDeleteAsync(ct);
        var values = await db
            .MetricValues.AsNoTracking()
            .Include(static v => v.Partitions)
            .ToListAsync(ct);

        var count = 0;
        foreach (
            var g in values.GroupBy(static v =>
            {
                var part = v.Partitions.FirstOrDefault();
                return (
                    DateKey: ToDateKey(DateOnly.FromDateTime(v.MetricValueDateTimeUtc.UtcDateTime)),
                    MetricId: v.MetricId.Value,
                    PartitionId: part?.EntityId,
                    Label: part?.EntityLabel ?? "All"
                );
            })
        )
        {
            var goals = g.Where(static x => x.MetricValueType == MetricValueType.Goal).ToList();
            _ = db.FactMetricsDaily.Add(
                new FactMetricDaily
                {
                    Id = Guid.CreateVersion7(),
                    DateKey = g.Key.DateKey,
                    MetricId = g.Key.MetricId,
                    PartitionEntityId = g.Key.PartitionId,
                    PartitionLabel = g.Key.Label,
                    MeasureSum = g.Where(static x => x.MetricValueType == MetricValueType.Measure)
                        .Sum(static x => x.YValue),
                    GoalSum = goals.Count > 0 ? goals.Sum(static x => x.YValue) : null,
                    SampleCount = g.Count(),
                    RefreshedAtUtc = nowUtc,
                }
            );
            count++;
        }

        _ = await db.SaveChangesAsync(ct);
        return count;
    }

    private async Task<int> RefreshFactRevenueMonthlyAsync(
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        if (
            !await sourceReader.TableExistsAsync("finance.financial_transactions", ct)
            || !await sourceReader.TableExistsAsync("finance.financial_transaction_details", ct)
            || !await sourceReader.TableExistsAsync("finance.financial_accounts", ct)
        )
        {
            return await db.FactRevenuesMonthly.CountAsync(ct);
        }

        _ = await db.FactRevenuesMonthly.ExecuteDeleteAsync(ct);
        const string sql =
            "SELECT date_trunc('month', t.\"TransactionDateTimeUtc\")::date AS month_date, a.\"BranchId\", "
            + "SUM(CASE WHEN a.\"IsTaxDeductible\" THEN d.\"Amount\" ELSE 0 END) AS deductible_sum, "
            + "SUM(CASE WHEN NOT a.\"IsTaxDeductible\" THEN d.\"Amount\" ELSE 0 END) AS non_deductible_sum, "
            + "COUNT(DISTINCT t.\"Id\")::int AS tx_count "
            + "FROM finance.financial_transactions t "
            + "INNER JOIN finance.financial_transaction_details d ON d.\"TransactionId\" = t.\"Id\" "
            + "INNER JOIN finance.financial_accounts a ON a.\"Id\" = d.\"AccountId\" GROUP BY 1, 2";

        var count = 0;
        await ReadRowsAsync(
            sql,
            async reader =>
            {
                var monthDate = await reader.GetFieldValueAsync<DateOnly>(0, ct);
                _ = db.FactRevenuesMonthly.Add(
                    new FactRevenueMonthly
                    {
                        Id = Guid.CreateVersion7(),
                        MonthDateKey = ToDateKey(monthDate),
                        MonthLabel = monthDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                        BranchId = await reader.IsDBNullAsync(1, ct) ? null : reader.GetGuid(1),
                        TaxDeductibleGiving = await reader.IsDBNullAsync(2, ct)
                            ? 0m
                            : reader.GetDecimal(2),
                        NonDeductibleDuesAndFees = await reader.IsDBNullAsync(3, ct)
                            ? 0m
                            : reader.GetDecimal(3),
                        TransactionCount = await reader.IsDBNullAsync(4, ct)
                            ? 0
                            : reader.GetInt32(4),
                        RefreshedAtUtc = nowUtc,
                    }
                );
                count++;
            },
            ct
        );

        _ = await db.SaveChangesAsync(ct);
        return count;
    }

    private async Task<int> RefreshFactAttendanceWeeklyAsync(
        DateTimeOffset nowUtc,
        CancellationToken ct
    )
    {
        if (
            !await sourceReader.TableExistsAsync("groups.attendance_occurrences", ct)
            || !await sourceReader.TableExistsAsync("groups.attendances", ct)
        )
        {
            return await db.FactAttendancesWeekly.CountAsync(ct);
        }

        _ = await db.FactAttendancesWeekly.ExecuteDeleteAsync(ct);
        const string sql =
            "SELECT o.\"WeekEndDate\", a.\"BranchId\", COUNT(DISTINCT o.\"Id\")::int AS occ_count, "
            + "COUNT(a.\"Id\") FILTER (WHERE a.\"DidAttend\")::int AS attended_count "
            + "FROM groups.attendance_occurrences o "
            + "LEFT JOIN groups.attendances a ON a.\"OccurrenceId\" = o.\"Id\" GROUP BY 1, 2";

        var count = 0;
        await ReadRowsAsync(
            sql,
            async reader =>
            {
                var weekEnd = await reader.GetFieldValueAsync<DateOnly>(0, ct);
                _ = db.FactAttendancesWeekly.Add(
                    new FactAttendanceWeekly
                    {
                        Id = Guid.CreateVersion7(),
                        WeekEndDateKey = ToDateKey(weekEnd),
                        WeekEndDate = weekEnd,
                        BranchId = await reader.IsDBNullAsync(1, ct) ? null : reader.GetGuid(1),
                        OccurrenceCount = await reader.IsDBNullAsync(2, ct)
                            ? 0
                            : reader.GetInt32(2),
                        AttendedCount = await reader.IsDBNullAsync(3, ct) ? 0 : reader.GetInt32(3),
                        RefreshedAtUtc = nowUtc,
                    }
                );
                count++;
            },
            ct
        );

        _ = await db.SaveChangesAsync(ct);
        return count;
    }

    private async Task ReadRowsAsync(
        string sql,
        Func<DbDataReader, Task> onRow,
        CancellationToken ct
    )
    {
        var conn = db.Database.GetDbConnection();
        var shouldClose = conn.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(ct);
        }

        try
        {
            await TenantRlsConnectionSynchronizer.EnsureCurrentTenantAsync(
                conn,
                db.Database.CurrentTransaction?.GetDbTransaction(),
                ct
            );
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                await onRow(reader);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    internal static int ToDateKey(DateOnly date)
    {
        return (date.Year * 10000) + (date.Month * 100) + date.Day;
    }
}
