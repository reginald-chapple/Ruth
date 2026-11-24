using System.Globalization;
using System.Net;
using Npgsql;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class ScheduledJobsTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _jobKeyColumns = ["JobKey", "job_key", "Key", "key"];
    private static readonly string[] _cronColumns =
    [
        "CronExpression",
        "cron_expression",
        "Cron",
        "cron",
    ];
    private static readonly string[] _historyCountColumns =
    [
        "HistoryCount",
        "history_count",
        "MaxHistoryCount",
        "max_history_count",
    ];

    [Fact]
    public async Task Feature07_SchedulerSchema_ContainsScheduledJobsAndHistoryTables()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);

        var jobsTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ProbeSqlCatalog.ScheduledJobTableCandidates
        );
        var historyTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ProbeSqlCatalog.ScheduledJobHistoryTableCandidates
        );

        Assert.NotNull(jobsTable);
        Assert.NotNull(historyTable);
    }

    [Fact]
    public async Task Feature07_ScheduledJobsTable_HasRequiredScheduleAndMetadataColumns()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var jobsTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ProbeSqlCatalog.ScheduledJobTableCandidates
        );
        Assert.NotNull(jobsTable);

        var pkColumns = await scope.Probe.GetPrimaryKeyColumnsAsync("scheduler", jobsTable);
        var columns = await scope.Probe.GetColumnsAsync("scheduler", jobsTable);

        Assert.NotEmpty(pkColumns);
        Assert.Contains(columns, c =>
            c.ColumnName.Contains("key", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(columns, c =>
            c.ColumnName.Contains("cron", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(columns, c =>
            c.ColumnName.Contains("utc", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Feature07_ScheduledJobHistoryTable_HasExecutionAndStatusColumns()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var historyTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ProbeSqlCatalog.ScheduledJobHistoryTableCandidates
        );
        Assert.NotNull(historyTable);

        var pkColumns = await scope.Probe.GetPrimaryKeyColumnsAsync("scheduler", historyTable);
        var columns = await scope.Probe.GetColumnsAsync("scheduler", historyTable);

        Assert.NotEmpty(pkColumns);
        Assert.Contains(columns, c =>
            c.ColumnName.Contains("status", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(columns, c =>
            c.ColumnName.Contains("utc", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Feature07_PostgresAdvisoryLock_CanBeAcquiredAndReleasedCleanly()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        const long lockKey = 7_000_042L;

        await using (var firstLease = await scope.Probe.TryAcquireAdvisoryLockAsync(lockKey))
        {
            Assert.True(firstLease.IsAcquired);

            await using var contendedLease = await scope.Probe.TryAcquireAdvisoryLockAsync(lockKey);
            Assert.False(contendedLease.IsAcquired);
        }

        await using var reacquiredLease = await scope.Probe.TryAcquireAdvisoryLockAsync(lockKey);
        Assert.True(reacquiredLease.IsAcquired);
    }

    [Fact]
    public async Task Feature07_SchedulerSchema_ScheduledJobsHaveValidCronAndHistoryLimit()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var jobsTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ProbeSqlCatalog.ScheduledJobTableCandidates
        );
        Assert.NotNull(jobsTable);

        var rows = await scope.Probe.ReadScheduledJobsAsync();
        if (rows.Count == 0)
        {
            await InsertProbeScheduledJobAsync(scope.ConnectionString);
            rows = await scope.Probe.ReadScheduledJobsAsync();
        }

        Assert.NotEmpty(rows);
        foreach (var row in rows)
        {
            Assert.True(Tier1TestSupport.TryGetAnyColumn(row, _jobKeyColumns, out var keyRaw));
            Assert.False(
                string.IsNullOrWhiteSpace(Convert.ToString(keyRaw, CultureInfo.InvariantCulture))
            );

            Assert.True(Tier1TestSupport.TryGetAnyColumn(row, _cronColumns, out var cronRaw));
            Assert.False(
                string.IsNullOrWhiteSpace(Convert.ToString(cronRaw, CultureInfo.InvariantCulture))
            );

            Assert.True(
                Tier1TestSupport.TryGetAnyColumn(row, _historyCountColumns, out var limitRaw)
            );
            var limit = Convert.ToInt32(limitRaw, CultureInfo.InvariantCulture);
            Assert.True(limit > 0);
        }
    }

    private static async Task InsertProbeScheduledJobAsync(string connectionString)
    {
        const string sql =
            "INSERT INTO scheduler.scheduled_jobs"
            + " (id, job_key, name, cron_expression, next_run_utc)"
            + " VALUES (@id, @jobKey, @name, @cron, @nextRunUtc);";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        _ = command.Parameters.AddWithValue("@id", Guid.CreateVersion7());
        _ = command.Parameters.AddWithValue("@jobKey", "tier1.schema.probe");
        _ = command.Parameters.AddWithValue("@name", "Tier1 Schema Probe Job");
        _ = command.Parameters.AddWithValue("@cron", "0 * * * *");
        _ = command.Parameters.AddWithValue("@nextRunUtc", DateTimeOffset.UtcNow.AddHours(1));
        _ = await command.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Feature07_HostStartupAndShutdown_WithSchedulerEngine_CompletesCleanly()
    {
        var dbName = await containers.CreateIsolatedDatabaseAsync();
        try
        {
            var connectionString = containers.GetConnectionString(dbName);
            var env = Tier1TestSupport.BuildProcessEnvironment(
                containers,
                connectionString,
                "Development",
                RuthHostFactory.SafeTestJwtSigningKey
            );

            await using var host = await HostProcessRunner.StartHostAsync(
                env,
                TimeSpan.FromSeconds(30)
            );
            using var response = await host.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var stopResult = await host.StopAndCollectAsync();
            Assert.False(stopResult.TimedOut);
        }
        finally
        {
            await containers.DropDatabaseAsync(dbName);
        }
    }
}
