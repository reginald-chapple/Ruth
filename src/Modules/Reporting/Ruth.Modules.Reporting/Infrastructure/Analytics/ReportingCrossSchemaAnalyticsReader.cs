using Npgsql;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Infrastructure.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Analytics;

internal sealed class ReportingCrossSchemaAnalyticsReader(
    ReportingDbContext db,
    ReportingDataViewExecutionEngine sourceReader
)
{
    public async Task<decimal> ExecuteAllowlistedSqlMetricAsync(
        string sourceQueryKey,
        CancellationToken ct
    )
    {
        return sourceQueryKey.ToLowerInvariant() switch
        {
            "people.total" => await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                db,
                "crm.people",
                "SELECT COUNT(*)::numeric FROM crm.people WHERE tenant_id = @tenantId AND \"IsDeceased\" = FALSE",
                ct
            ),
            "groups.active" => await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                db,
                "groups.groups",
                "SELECT COUNT(*)::numeric FROM groups.groups WHERE tenant_id = @tenantId AND \"IsActive\" = TRUE",
                ct
            ),
            "attendance.total" => await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                db,
                "groups.attendances",
                "SELECT COUNT(*)::numeric FROM groups.attendances WHERE tenant_id = @tenantId AND \"DidAttend\" = TRUE",
                ct
            ),
            "finance.deductible-total" =>
                await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                    db,
                    "finance.financial_transaction_details",
                    "SELECT COALESCE(SUM(d.\"Amount\"), 0)::numeric FROM finance.financial_transaction_details d "
                        + "INNER JOIN finance.financial_accounts a ON a.tenant_id = d.tenant_id AND a.\"Id\" = d.\"AccountId\" "
                        + "WHERE d.tenant_id = @tenantId AND a.\"IsTaxDeductible\" = TRUE",
                    ct
                ),
            "finance.non-deductible-total" =>
                await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                    db,
                    "finance.financial_transaction_details",
                    "SELECT COALESCE(SUM(d.\"Amount\"), 0)::numeric FROM finance.financial_transaction_details d "
                        + "INNER JOIN finance.financial_accounts a ON a.tenant_id = d.tenant_id AND a.\"Id\" = d.\"AccountId\" "
                        + "WHERE d.tenant_id = @tenantId AND a.\"IsTaxDeductible\" = FALSE",
                    ct
                ),
            "lms.completions" => await ReportingCrossSchemaSqlRunner.ScalarDecimalIfExistsAsync(
                db,
                "lms.learning_participants",
                "SELECT COUNT(*)::numeric FROM lms.learning_participants WHERE tenant_id = @tenantId AND \"LearningCompletionStatus\" = 'Pass'",
                ct
            ),
            _ => 0m,
        };
    }

    public async Task<RevenueAndImpactOverviewResponse> LoadRevenueAndImpactAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? branchId,
        CancellationToken ct
    )
    {
        var (deductible, nonDeductible, periods) = await LoadRevenuePeriodsAsync(
            startUtc,
            endUtc,
            branchId,
            ct
        );
        var (mrr, activeRecurring) = await LoadRecurringMrrAsync(ct);
        var retention = await LoadRetentionRateAsync(startUtc, endUtc, ct);
        var attendance = await LoadAttendanceTotalAsync(startUtc, endUtc, branchId, ct);
        var assistanceTotal = await LoadAssistanceDisbursementsAsync(startUtc, endUtc, ct);
        var outcomes = await LoadProgramCompletionsAsync(startUtc, endUtc, ct);

        return new RevenueAndImpactOverviewResponse(
            startUtc,
            endUtc,
            branchId,
            deductible,
            nonDeductible,
            mrr,
            activeRecurring,
            retention,
            attendance,
            assistanceTotal,
            outcomes,
            periods
        );
    }

    public async Task<IReadOnlyList<ExecutiveKpiCardDto>> LoadExecutiveKpisAsync(
        DateTimeOffset nowUtc,
        int windowDays,
        Guid? branchId,
        CancellationToken ct
    )
    {
        var clamped = Math.Clamp(windowDays, 7, 365);
        var windowStart = nowUtc.AddDays(-clamped);
        var prevStart = windowStart.AddDays(-clamped);

        var totalPeople = await ExecuteAllowlistedSqlMetricAsync("people.total", ct);
        var activeGroups = await ExecuteAllowlistedSqlMetricAsync("groups.active", ct);
        var currentAttendance = await LoadAttendanceTotalAsync(windowStart, nowUtc, branchId, ct);
        var priorAttendance = await LoadAttendanceTotalAsync(prevStart, windowStart, branchId, ct);
        var overview = await LoadRevenueAndImpactAsync(windowStart, nowUtc, branchId, ct);
        var priorOverview = await LoadRevenueAndImpactAsync(prevStart, windowStart, branchId, ct);

        return ExecutiveKpiCardBuilder.BuildKpiCards(
            clamped,
            totalPeople,
            activeGroups,
            currentAttendance,
            priorAttendance,
            overview,
            priorOverview
        );
    }

    private async Task<(
        decimal Deductible,
        decimal NonDeductible,
        IReadOnlyList<RevenueAndImpactPeriodDto> Periods
    )> LoadRevenuePeriodsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? branchId,
        CancellationToken ct
    )
    {
        if (
            !await sourceReader.TableExistsAsync("finance.financial_transactions", ct)
            || !await sourceReader.TableExistsAsync("finance.financial_transaction_details", ct)
            || !await sourceReader.TableExistsAsync("finance.financial_accounts", ct)
        )
        {
            return (0m, 0m, []);
        }

        const string sql =
            "SELECT to_char(date_trunc('month', t.\"TransactionDateTimeUtc\"), 'YYYY-MM') AS period_label, "
            + "SUM(CASE WHEN a.\"IsTaxDeductible\" THEN d.\"Amount\" ELSE 0 END) AS deductible_sum, "
            + "SUM(CASE WHEN NOT a.\"IsTaxDeductible\" THEN d.\"Amount\" ELSE 0 END) AS non_deductible_sum "
            + "FROM finance.financial_transactions t "
            + "INNER JOIN finance.financial_transaction_details d ON d.tenant_id = t.tenant_id AND d.\"TransactionId\" = t.\"Id\" "
            + "INNER JOIN finance.financial_accounts a ON a.tenant_id = t.tenant_id AND a.\"Id\" = d.\"AccountId\" "
            + "WHERE t.tenant_id = @tenantId AND t.\"TransactionDateTimeUtc\" >= @startUtc AND t.\"TransactionDateTimeUtc\" <= @endUtc "
            + "AND (@branchId IS NULL OR a.\"BranchId\" = @branchId) GROUP BY 1 ORDER BY 1";

        List<RevenueAndImpactPeriodDto> periods = [];
        var deductibleTotal = 0m;
        var nonDeductibleTotal = 0m;
        await ReportingCrossSchemaSqlRunner.ExecuteWithTenantAsync(
            db,
            async (conn, tenantId) =>
            {
                await using var cmd = ReportingCrossSchemaSqlRunner.CreateTenantCommand(
                    conn,
                    sql,
                    tenantId
                );
                _ = cmd.Parameters.Add(new NpgsqlParameter("startUtc", startUtc));
                _ = cmd.Parameters.Add(new NpgsqlParameter("endUtc", endUtc));
                _ = cmd.Parameters.Add(
                    new NpgsqlParameter("branchId", (object?)branchId ?? DBNull.Value)
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid,
                    }
                );
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var label = reader.GetString(0);
                    var ded = await reader.IsDBNullAsync(1, ct) ? 0m : reader.GetDecimal(1);
                    var nonDed = await reader.IsDBNullAsync(2, ct) ? 0m : reader.GetDecimal(2);
                    deductibleTotal += ded;
                    nonDeductibleTotal += nonDed;
                    periods.Add(new RevenueAndImpactPeriodDto(label, ded, nonDed, 0, 0));
                }
            },
            ct
        );

        return (deductibleTotal, nonDeductibleTotal, periods);
    }

    private async Task<(decimal Mrr, int ActiveCount)> LoadRecurringMrrAsync(CancellationToken ct)
    {
        if (
            !await sourceReader.TableExistsAsync("finance.recurring_schedules", ct)
            || !await sourceReader.TableExistsAsync("finance.recurring_schedule_splits", ct)
        )
        {
            return (0m, 0);
        }

        const string sql =
            "SELECT COALESCE(SUM(CASE r.\"Frequency\" WHEN 'Weekly' THEN s.\"Amount\" * 4.3333 "
            + "WHEN 'BiWeekly' THEN s.\"Amount\" * 2.1667 WHEN 'TwiceMonthly' THEN s.\"Amount\" * 2.0 "
            + "WHEN 'Quarterly' THEN s.\"Amount\" / 3.0 WHEN 'Annual' THEN s.\"Amount\" / 12.0 ELSE s.\"Amount\" END), 0)::numeric, "
            + "COUNT(DISTINCT r.\"Id\")::int FROM finance.recurring_schedules r "
            + "INNER JOIN finance.recurring_schedule_splits s ON s.tenant_id = r.tenant_id AND s.\"ScheduleId\" = r.\"Id\" "
            + "WHERE r.tenant_id = @tenantId AND r.\"Status\" = 'Active'";

        var result = (Mrr: 0m, ActiveCount: 0);
        await ReportingCrossSchemaSqlRunner.ExecuteWithTenantAsync(
            db,
            async (conn, tenantId) =>
            {
                await using var cmd = ReportingCrossSchemaSqlRunner.CreateTenantCommand(
                    conn,
                    sql,
                    tenantId
                );
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var rawMrr = await reader.IsDBNullAsync(0, ct) ? 0m : reader.GetDecimal(0);
                    var active = await reader.IsDBNullAsync(1, ct) ? 0 : reader.GetInt32(1);
                    result = (Math.Round(rawMrr, 2, MidpointRounding.AwayFromZero), active);
                }
            },
            ct
        );

        return result;
    }

    private async Task<decimal> LoadRetentionRateAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct
    )
    {
        if (!await sourceReader.TableExistsAsync("finance.financial_transactions", ct))
        {
            return 0m;
        }

        var priorStart = startUtc - (endUtc - startUtc);
        const string sql =
            "WITH prior_people AS (SELECT DISTINCT \"AuthorizedPersonId\" FROM finance.financial_transactions "
            + "WHERE tenant_id = @tenantId AND \"AuthorizedPersonId\" IS NOT NULL AND \"TransactionDateTimeUtc\" >= @priorStart AND \"TransactionDateTimeUtc\" < @startUtc), "
            + "curr_people AS (SELECT DISTINCT \"AuthorizedPersonId\" FROM finance.financial_transactions "
            + "WHERE tenant_id = @tenantId AND \"AuthorizedPersonId\" IS NOT NULL AND \"TransactionDateTimeUtc\" >= @startUtc AND \"TransactionDateTimeUtc\" <= @endUtc) "
            + "SELECT CASE WHEN (SELECT COUNT(*) FROM prior_people) = 0 THEN 100.0 "
            + "ELSE ROUND((SELECT COUNT(*) FROM prior_people p INNER JOIN curr_people c ON c.\"AuthorizedPersonId\" = p.\"AuthorizedPersonId\")::numeric * 100.0 / (SELECT COUNT(*) FROM prior_people), 2) END";

        var rate = 0m;
        await ReportingCrossSchemaSqlRunner.ExecuteWithTenantAsync(
            db,
            async (conn, tenantId) =>
            {
                await using var cmd = ReportingCrossSchemaSqlRunner.CreateTenantCommand(
                    conn,
                    sql,
                    tenantId
                );
                _ = cmd.Parameters.Add(new NpgsqlParameter("priorStart", priorStart));
                _ = cmd.Parameters.Add(new NpgsqlParameter("startUtc", startUtc));
                _ = cmd.Parameters.Add(new NpgsqlParameter("endUtc", endUtc));
                var val = await cmd.ExecuteScalarAsync(ct);
                rate = val is decimal d ? d : 0m;
            },
            ct
        );

        return rate;
    }

    private async Task<int> LoadAttendanceTotalAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? branchId,
        CancellationToken ct
    )
    {
        if (!await sourceReader.TableExistsAsync("groups.attendances", ct))
        {
            return 0;
        }

        const string sql =
            "SELECT COUNT(*)::int FROM groups.attendances WHERE tenant_id = @tenantId AND \"DidAttend\" = TRUE "
            + "AND \"StartDateTimeUtc\" >= @startUtc AND \"StartDateTimeUtc\" <= @endUtc "
            + "AND (@branchId IS NULL OR \"BranchId\" = @branchId)";

        var total = 0;
        await ReportingCrossSchemaSqlRunner.ExecuteWithTenantAsync(
            db,
            async (conn, tenantId) =>
            {
                await using var cmd = ReportingCrossSchemaSqlRunner.CreateTenantCommand(
                    conn,
                    sql,
                    tenantId
                );
                _ = cmd.Parameters.Add(new NpgsqlParameter("startUtc", startUtc));
                _ = cmd.Parameters.Add(new NpgsqlParameter("endUtc", endUtc));
                _ = cmd.Parameters.Add(
                    new NpgsqlParameter("branchId", (object?)branchId ?? DBNull.Value)
                    {
                        NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid,
                    }
                );
                var val = await cmd.ExecuteScalarAsync(ct);
                total = val is int i ? i : 0;
            },
            ct
        );

        return total;
    }

    private Task<decimal> LoadAssistanceDisbursementsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct
    )
    {
        return ReportingCrossSchemaSqlRunner.ScalarDecimalWithRangeAsync(
            db,
            "finance.assistance_disbursements",
            "SELECT COALESCE(SUM(\"Amount\"), 0)::numeric FROM finance.assistance_disbursements "
                + "WHERE tenant_id = @tenantId AND \"DisbursedAtUtc\" >= @startUtc AND \"DisbursedAtUtc\" <= @endUtc",
            startUtc,
            endUtc,
            ct
        );
    }

    private async Task<int> LoadProgramCompletionsAsync(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken ct
    )
    {
        var journeys = (int)
            await ReportingCrossSchemaSqlRunner.ScalarDecimalWithRangeAsync(
                db,
                "engage.journey_completions",
                "SELECT COUNT(*)::numeric FROM engage.journey_completions "
                    + "WHERE tenant_id = @tenantId AND \"CompletedDate\" IS NOT NULL "
                    + "AND \"CompletedDate\" >= @startUtc::date AND \"CompletedDate\" <= @endUtc::date",
                startUtc,
                endUtc,
                ct
            );
        var lms = (int)
            await ReportingCrossSchemaSqlRunner.ScalarDecimalWithRangeAsync(
                db,
                "lms.learning_participants",
                "SELECT COUNT(*)::numeric FROM lms.learning_participants "
                    + "WHERE tenant_id = @tenantId AND \"LearningCompletionStatus\" = 'Pass' "
                    + "AND \"LearningCompletionDateTimeUtc\" >= @startUtc AND \"LearningCompletionDateTimeUtc\" <= @endUtc",
                startUtc,
                endUtc,
                ct
            );
        return journeys + lms;
    }
}
