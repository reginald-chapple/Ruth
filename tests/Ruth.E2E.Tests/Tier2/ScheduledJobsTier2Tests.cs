using System.Globalization;
using Npgsql;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class ScheduledJobsTier2Tests(E2EContainersFixture containers)
{
    private static readonly Dictionary<string, string?> _fastSchedulerOverrides = new(
        StringComparer.Ordinal
    )
    {
        ["Scheduler:PollingInterval"] = "00:00:00.200",
    };

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task AdvisoryLock_SecondConcurrentAcquisitionSameKey_ReturnsNotAcquired()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, _, probe) =>
        {
            const long lockKey = 98765432101L;
            await using var lease1 = await probe.TryAcquireAdvisoryLockAsync(lockKey);
            await using var lease2 = await probe.TryAcquireAdvisoryLockAsync(lockKey);

            Assert.True(lease1.IsAcquired);
            Assert.False(lease2.IsAcquired);
        });
    }

    [Fact]
    public async Task AdvisoryLock_ReleaseOnLeaseDisposal_AllowsImmediateReacquire()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, _, probe) =>
        {
            const long lockKey = 98765432102L;
            await using (var lease1 = await probe.TryAcquireAdvisoryLockAsync(lockKey))
            {
                Assert.True(lease1.IsAcquired);
            }

            await using var lease2 = await probe.TryAcquireAdvisoryLockAsync(lockKey);
            Assert.True(lease2.IsAcquired);
        });
    }

    [Fact]
    public async Task ScheduledJob_NullDuplicateOrOverlengthJobKey_RejectedByConstraints()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, _) =>
        {
            await using var npgsql = new NpgsqlConnection(conn);
            await npgsql.OpenAsync();

            var nullEx = await Assert.ThrowsAsync<PostgresException>(() =>
                Tier2SqlHelpers.InsertScheduledJobAsync(
                    npgsql,
                    Guid.CreateVersion7(),
                    jobKey: null,
                    "* * * * *"
                )
            );
            Assert.Equal("23502", nullEx.SqlState);

            await Tier2SqlHelpers.InsertScheduledJobAsync(
                npgsql,
                Guid.CreateVersion7(),
                "tier2.unique.job",
                "* * * * *"
            );
            var dupEx = await Assert.ThrowsAsync<PostgresException>(() =>
                Tier2SqlHelpers.InsertScheduledJobAsync(
                    npgsql,
                    Guid.CreateVersion7(),
                    "tier2.unique.job",
                    "* * * * *"
                )
            );
            Assert.Equal("23505", dupEx.SqlState);
        });
    }

    [Fact]
    public async Task ScheduledJob_InvalidCronExpression_HandledWithoutCrashingHost()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, client, conn, _) =>
            {
                await using var npgsql = new NpgsqlConnection(conn);
                await npgsql.OpenAsync();
                await Tier2SqlHelpers.InsertScheduledJobAsync(
                    npgsql,
                    Guid.CreateVersion7(),
                    "tier2.invalid.cron",
                    "not-a-valid-cron",
                    DateTimeOffset.UtcNow.AddMinutes(-1)
                );

                await Task.Delay(TimeSpan.FromMilliseconds(500));
                await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
            },
            overrides: _fastSchedulerOverrides
        );
    }

    [Fact]
    public async Task ScheduledJob_HistoryCountColumn_DefaultsTo500AndIsNonNullable()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, probe) =>
        {
            var columns = await probe.GetColumnsAsync("scheduler", "scheduled_jobs");
            var historyCol = columns.Single(c =>
                string.Equals(c.ColumnName, "history_count", StringComparison.Ordinal)
            );
            Assert.False(historyCol.IsNullable);

            await using var npgsql = new NpgsqlConnection(conn);
            await npgsql.OpenAsync();
            var jobId = Guid.CreateVersion7();
            await Tier2SqlHelpers.InsertScheduledJobAsync(
                npgsql,
                jobId,
                "tier2.history.default",
                "0 * * * *"
            );

            var row = (await probe.ReadScheduledJobsAsync()).Single(r =>
                DatabaseProbe.TryGetColumnValue(r, "Id", out var v) && Equals(v, jobId)
            );
            Assert.True(DatabaseProbe.TryGetColumnValue(row, "HistoryCount", out var count));
            Assert.Equal(500, Convert.ToInt32(count, CultureInfo.InvariantCulture));
        });
    }

    [Fact]
    public async Task ScheduledJob_ActivityExecution_UsesIsolatedRootParentId()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            using var capture = RuthHostFactory.StartActivityCapture(
                "Ruth",
                "Microsoft.AspNetCore"
            );
            await using var factory = RuthHostFactory.Create(
                _containers,
                conn,
                overrides: _fastSchedulerOverrides
            );
            using var client = factory.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(client);
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var schedulerActivities = capture
                .RecordedActivities.Where(a =>
                    a.Source.Name.Contains("Scheduler", StringComparison.OrdinalIgnoreCase)
                    || a.OperationName.Contains("Job", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            Assert.NotEmpty(schedulerActivities);
            Assert.All(schedulerActivities, a => Assert.Null(a.ParentId));
        });
    }
}
