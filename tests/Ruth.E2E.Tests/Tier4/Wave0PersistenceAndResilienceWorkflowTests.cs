using System.Net;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier4;

[Collection(E2ETestCollection.Name)]
public sealed class Wave0PersistenceAndResilienceWorkflowTests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Scenario3_TransactionalOnboarding_OutboxCapture_Dispatch_AndInboxIdempotency()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            using var onboardDoc = await WorkflowTestSupport.CompleteOnboardingAsync(client);
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var initialOutbox = Assert.Single(await probe.ReadIdentityOutboxMessagesAsync());
            AssertOutboxPayloadAndUuidV7(initialOutbox, "Grace Community Church");

            var processed = await WorkflowTestSupport.WaitForProcessedOutboxAsync(
                probe,
                TimeSpan.FromSeconds(20)
            );
            var occurredUtc = WorkflowTestSupport.ReadUtcDateTimeOffset(processed, "OccurredOnUtc");
            var processedUtc = WorkflowTestSupport.ReadUtcDateTimeOffset(
                processed,
                "ProcessedOnUtc"
            );
            Assert.True(processedUtc >= occurredUtc);

            var inboxTable = await probe.ResolveFirstExistingTableAsync(
                "identity",
                ["InboxMessages", "inbox_messages", "InboxMessage", "inbox_message"]
            );
            Assert.NotNull(inboxTable);

            var inboxPk = await probe.GetPrimaryKeyColumnsAsync("identity", inboxTable);
            Assert.Equal(2, inboxPk.Count);
        });
    }

    [Fact]
    public async Task Scenario4_DualHostInstances_SharedPostgres_SchedulerLock_AndHistoryBounds()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var envA = WorkflowTestSupport.BuildHostEnvironment(
                _containers,
                databaseName,
                "Development",
                RuthHostFactory.SafeTestJwtSigningKey
            );
            var envB = WorkflowTestSupport.BuildHostEnvironment(
                _containers,
                databaseName,
                "Development",
                RuthHostFactory.SafeTestJwtSigningKey
            );

            await using var hostA = await HostProcessRunner.StartHostAsync(
                envA,
                TimeSpan.FromSeconds(30)
            );
            await using var hostB = await HostProcessRunner.StartHostAsync(
                envB,
                TimeSpan.FromSeconds(30)
            );

            using var healthA = await hostA.GetAsync("/health");
            using var healthB = await hostB.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, healthA.StatusCode);
            Assert.Equal(HttpStatusCode.OK, healthB.StatusCode);

            await AssertSharedSchedulerInvariantsAsync(databaseName);
        });
    }

    [Fact]
    public async Task Scenario6_DependencyOutageAndRecovery_HealthReadinessTransitions()
    {
        await WorkflowTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            var targetUri = new Uri(_containers.RustFsEndpoint, UriKind.Absolute);
            await using var proxy = new TcpForwardingProxy(targetUri);
            proxy.Start();

            await using var host = RuthHostFactory.CreateWithCustomEndpoints(
                _containers.GetConnectionString(databaseName),
                _containers.RedisConnectionString,
                proxy.EndpointUrl,
                jwtSigningKey: RuthHostFactory.SafeTestJwtSigningKey
            );
            using var client = host.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            await WorkflowTestSupport.WaitForHealthStatusAsync(
                client,
                HttpStatusCode.OK,
                TimeSpan.FromSeconds(15)
            );

            await proxy.StopAsync();

            using var liveDuringOutage = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveDuringOutage.StatusCode);

            await WorkflowTestSupport.WaitForHealthStatusAsync(
                client,
                HttpStatusCode.ServiceUnavailable,
                TimeSpan.FromSeconds(15)
            );

            proxy.Start();

            await WorkflowTestSupport.WaitForHealthStatusAsync(
                client,
                HttpStatusCode.OK,
                TimeSpan.FromSeconds(15)
            );
        });
    }

    private static void AssertOutboxPayloadAndUuidV7(
        IReadOnlyDictionary<string, object?> row,
        string expectedOrganizationName
    )
    {
        Assert.True(DatabaseProbe.TryGetColumnValue(row, "Id", out var idObj));
        var id = Assert.IsType<Guid>(idObj);
        TestOnboardingPayloads.AssertIsUuidV7(id);

        Assert.True(DatabaseProbe.TryGetColumnValue(row, "Payload", out var payloadObj));
        var payloadJson = Assert.IsType<string>(payloadObj);
        using var doc = JsonDocument.Parse(payloadJson);
        Assert.Contains(expectedOrganizationName, payloadJson, StringComparison.Ordinal);

        var occurred = WorkflowTestSupport.ReadUtcDateTimeOffset(row, "OccurredOnUtc");
        TestOnboardingPayloads.AssertUtcTimestamp(occurred);
    }

    private async Task AssertSharedSchedulerInvariantsAsync(string databaseName)
    {
        var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
        Assert.True(await probe.SchemaExistsAsync("scheduler"));

        var jobsTable = await probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ["ScheduledJobs", "scheduled_jobs"]
        );
        Assert.NotNull(jobsTable);

        var historyTable = await probe.ResolveFirstExistingTableAsync(
            "scheduler",
            ["ScheduledJobHistories", "scheduled_job_histories", "ScheduledJobHistory"]
        );
        Assert.NotNull(historyTable);

        var jobIndexes = await probe.GetIndexesAsync("scheduler", jobsTable);
        Assert.Contains(jobIndexes, idx =>
            idx.Definition.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
        );

        await AssertAdvisoryLockExclusionAsync(probe, 741_852_963L);

        var jobs = await probe.ReadScheduledJobsAsync();
        var historyCount = await probe.CountScheduledJobHistoryAsync();
        var distinctKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var job in jobs)
        {
            if (
                DatabaseProbe.TryGetColumnValue(job, "JobKey", out var keyObj)
                && keyObj is string jobKey
            )
            {
                Assert.True(distinctKeys.Add(jobKey), $"Duplicate ScheduledJob JobKey '{jobKey}'.");
            }
        }

        Assert.True(historyCount <= Math.Max(1, jobs.Count) * 500L);
    }

    private static async Task AssertAdvisoryLockExclusionAsync(DatabaseProbe probe, long lockKey)
    {
        await using var first = await probe.TryAcquireAdvisoryLockAsync(lockKey);
        Assert.True(first.IsAcquired);

        await using var second = await probe.TryAcquireAdvisoryLockAsync(lockKey);
        Assert.False(second.IsAcquired);
    }
}
