using System.Net;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier3;

[Collection(E2ETestCollection.Name)]
public sealed class PersistenceOutboxAndSchedulerPairwiseTests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task OnboardingPost_AtomicallyPersists_OutboxMessage_WithUuidV7_Jsonb_AndUtc()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(client);
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var tableName = await probe.ResolveFirstExistingTableAsync(
                "identity",
                ["OutboxMessages", "outbox_messages", "OutboxMessage", "outbox_message"]
            );
            Assert.NotNull(tableName);

            var columns = await probe.GetColumnsAsync("identity", tableName);
            var payloadColumn = Assert.Single(columns, c =>
                string.Equals(c.ColumnName, "payload", StringComparison.OrdinalIgnoreCase)
            );
            Assert.Equal("jsonb", payloadColumn.DataType, StringComparer.OrdinalIgnoreCase);

            var rows = await probe.ReadIdentityOutboxMessagesAsync();
            var row = Assert.Single(rows);
            PairwiseTestSupport.AssertOutboxRowValid(row, "Grace Community Church");
        });
    }

    [Fact]
    public async Task OutboxDispatcherService_ProcessesOutbox_And_InboxEnforcesCompositeKey()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(client);
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var processedRow = await PairwiseTestSupport.WaitForProcessedOutboxAsync(
                probe,
                TimeSpan.FromSeconds(20)
            );

            var occurred = PairwiseTestSupport.ReadUtcDateTimeOffset(processedRow, "OccurredOnUtc");
            var processed = PairwiseTestSupport.ReadUtcDateTimeOffset(
                processedRow,
                "ProcessedOnUtc"
            );
            Assert.True(processed >= occurred);
            Assert.True(
                DatabaseProbe.TryGetColumnValue(processedRow, "Error", out var err) && err is null
            );

            var inboxTable = await probe.ResolveFirstExistingTableAsync(
                "identity",
                ["InboxMessages", "inbox_messages", "InboxMessage", "inbox_message"]
            );
            Assert.NotNull(inboxTable);

            var pkCols = await probe.GetPrimaryKeyColumnsAsync("identity", inboxTable);
            Assert.Equal(2, pkCols.Count);
        });
    }

    [Fact]
    public async Task ModuleDbContext_Pg18Extensions_IsolateIdentity_AndSchedulerSchemas()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();
            using var liveResponse = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var extensions = await probe.GetInstalledExtensionsAsync();
            Assert.Contains("uuid-ossp", extensions, StringComparer.Ordinal);
            Assert.Contains("postgis", extensions, StringComparer.Ordinal);
            Assert.Contains("vector", extensions, StringComparer.Ordinal);
            Assert.Contains("age", extensions, StringComparer.Ordinal);

            var identityTables = await probe.GetTablesInSchemaAsync("identity");
            var schedulerTables = await probe.GetTablesInSchemaAsync("scheduler");

            Assert.NotEmpty(identityTables);
            Assert.NotEmpty(schedulerTables);
            Assert.DoesNotContain(identityTables, t =>
                t.Contains("scheduled", StringComparison.OrdinalIgnoreCase)
            );
            Assert.DoesNotContain(schedulerTables, t =>
                t.Contains("user", StringComparison.OrdinalIgnoreCase)
            );
        });
    }

    [Fact]
    public async Task AuditInterceptor_StampsUtcTimestamps_Consistently_AcrossOrgAndUser()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            var minUtc = DateTimeOffset.UtcNow.AddSeconds(-5);
            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(client);
            var maxUtc = DateTimeOffset.UtcNow.AddSeconds(5);

            Assert.True(onboardDoc.RootElement.TryGetProperty("completedOnUtc", out _));

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var orgRow = Assert.Single(await probe.ReadOrganizationProfilesAsync());
            var userRow = Assert.Single(await probe.ReadUserAccountsAsync());

            var orgCreated = PairwiseTestSupport.ReadUtcDateTimeOffset(orgRow, "CreatedOnUtc");
            var userCreated = PairwiseTestSupport.ReadUtcDateTimeOffset(userRow, "CreatedOnUtc");

            TestOnboardingPayloads.AssertUtcTimestampInRange(orgCreated, minUtc, maxUtc);
            TestOnboardingPayloads.AssertUtcTimestampInRange(userCreated, minUtc, maxUtc);
        });
    }

    [Fact]
    public async Task AdvisoryLock_PreventsConcurrentLease_AcrossDualHostInstances()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var hostA = RuthHostFactory.Create(_containers, databaseName);
            await using var hostB = RuthHostFactory.Create(_containers, databaseName);
            using var clientA = hostA.CreateClient();
            using var clientB = hostB.CreateClient();

            using var resA = await clientA.GetAsync("/health/live");
            using var resB = await clientB.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, resA.StatusCode);
            Assert.Equal(HttpStatusCode.OK, resB.StatusCode);

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            const long testLockKey = 982_451_653L;

            await AssertAdvisoryLockContentionAsync(probe, testLockKey);

            await using var leaseAfter = await probe.TryAcquireAdvisoryLockAsync(testLockKey);
            Assert.True(leaseAfter.IsAcquired);
        });
    }

    private static async Task AssertAdvisoryLockContentionAsync(
        DatabaseProbe probe,
        long testLockKey
    )
    {
        await using var lease1 = await probe.TryAcquireAdvisoryLockAsync(testLockKey);
        Assert.True(lease1.IsAcquired);

        await using var lease2 = await probe.TryAcquireAdvisoryLockAsync(testLockKey);
        Assert.False(lease2.IsAcquired);
    }

    [Fact]
    public async Task OnboardingAdversarialEncoding_PreservesDbValue_AndEscapesShellHtml()
    {
        await PairwiseTestSupport.RunWithIsolatedDatabaseAsync(_containers, async databaseName =>
        {
            await using var host = RuthHostFactory.Create(_containers, databaseName);
            using var client = host.CreateClient();

            var payload = TestOnboardingPayloads.CreateAdversarialEncodingPayload();
            var expectedOrgName = payload["organizationName"]!.GetValue<string>();
            using var onboardDoc = await PairwiseTestSupport.CompleteOnboardingAsync(
                client,
                payload
            );
            Assert.True(onboardDoc.RootElement.TryGetProperty("organizationId", out _));

            var probe = new DatabaseProbe(_containers.GetConnectionString(databaseName));
            var orgRow = Assert.Single(await probe.ReadOrganizationProfilesAsync());
            Assert.True(DatabaseProbe.TryGetColumnValue(orgRow, "Name", out var storedName));
            Assert.Equal(expectedOrgName, storedName?.ToString());

            using var homeResponse = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);
            var html = await homeResponse.Content.ReadAsStringAsync();
            Assert.DoesNotContain("<Church>", html, StringComparison.Ordinal);
        });
    }
}
