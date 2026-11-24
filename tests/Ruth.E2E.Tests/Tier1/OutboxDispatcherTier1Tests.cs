using System.Globalization;
using System.Net;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class OutboxDispatcherTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _processedColumns =
    [
        "ProcessedOnUtc",
        "processed_on_utc",
        "ProcessedAtUtc",
        "processed_at_utc",
    ];
    private static readonly string[] _occurredColumns =
    [
        "OccurredOnUtc",
        "occurred_on_utc",
        "CreatedOnUtc",
        "created_on_utc",
    ];
    private static readonly string[] _errorColumns = ["Error", "error", "LastError", "last_error"];
    private static readonly string[] _retryColumns =
    [
        "RetryCount",
        "retry_count",
        "AttemptCount",
        "attempt_count",
    ];

    [Fact]
    public async Task Feature06_OutboxDispatcher_ProcessesOrganizationOnboardedEvent()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var processedRow = await OnboardAndWaitOutboxAsync(scope);
        Assert.NotNull(processedRow);
    }

    [Fact]
    public async Task Feature06_OutboxDispatcher_PopulatesUtcProcessedTimestampAfterOccurredOn()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var processedRow = await OnboardAndWaitOutboxAsync(scope);

        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(processedRow, _processedColumns, out var processedRaw)
        );
        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(processedRow, _occurredColumns, out var occurredRaw)
        );

        var processedAt = Tier1TestSupport.ParseRequiredUtcTimestamp(processedRaw);
        var occurredAt = Tier1TestSupport.ParseRequiredUtcTimestamp(occurredRaw);

        TestOnboardingPayloads.AssertUtcTimestamp(processedAt);
        Assert.True(processedAt >= occurredAt);
    }

    [Fact]
    public async Task Feature06_OutboxDispatcher_LeavesZeroErrorsOnHappyPath()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var processedRow = await OnboardAndWaitOutboxAsync(scope);

        if (Tier1TestSupport.TryGetAnyColumn(processedRow, _errorColumns, out var errorRaw))
        {
            var errorText = Convert.ToString(errorRaw, CultureInfo.InvariantCulture);
            Assert.True(string.IsNullOrEmpty(errorText), $"Unexpected outbox error: {errorText}");
        }

        if (Tier1TestSupport.TryGetAnyColumn(processedRow, _retryColumns, out var retryRaw))
        {
            Assert.Equal(0, Convert.ToInt32(retryRaw, CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public async Task Feature06_OutboxDispatcher_KeepsHostResponsiveAfterDispatch()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        _ = await OnboardAndWaitOutboxAsync(scope);

        using var client = scope.Factory.CreateClient();
        using var liveResponse = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
    }

    [Fact]
    public async Task Feature06_OutboxDispatcher_ShutsDownCleanlyAfterProcessing()
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
            using var httpClient = host.CreateNoRedirectClient();
            using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(httpClient);
            Assert.NotNull(onboardingDoc);

            var probe = new DatabaseProbe(connectionString);
            var processedRow = await Tier1TestSupport.WaitForProcessedOutboxRowAsync(
                probe,
                _processedColumns,
                TimeSpan.FromSeconds(20)
            );
            Assert.NotNull(processedRow);

            var stopResult = await host.StopAndCollectAsync();
            Assert.False(stopResult.TimedOut);
        }
        finally
        {
            await containers.DropDatabaseAsync(dbName);
        }
    }

    private static async Task<IReadOnlyDictionary<string, object?>> OnboardAndWaitOutboxAsync(
        IsolatedHostScope scope
    )
    {
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var processedRow = await Tier1TestSupport.WaitForProcessedOutboxRowAsync(
            scope.Probe,
            _processedColumns,
            TimeSpan.FromSeconds(20)
        );
        Assert.NotNull(processedRow);
        return processedRow;
    }
}
