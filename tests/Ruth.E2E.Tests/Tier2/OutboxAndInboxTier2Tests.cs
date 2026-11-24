using Npgsql;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class OutboxAndInboxTier2Tests(E2EContainersFixture containers)
{
    private static readonly string[] _inboxPkColumns = ["message_id", "consumer_name"];

    private static readonly Dictionary<string, string?> _fastOutbox = new(StringComparer.Ordinal)
    {
        ["Outbox:PollingInterval"] = "00:00:00.200",
        ["Outbox:BaseRetryDelay"] = "00:00:00.100",
        ["Outbox:MaxRetries"] = "2",
    };

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task InboxMessage_DuplicateCompositePrimaryKey_ThrowsUniqueViolation23505()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, _) =>
        {
            var messageId = Guid.CreateVersion7();
            await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
            await Tier2SqlHelpers.InsertInboxAsync(npgsql, messageId, "ConsumerA");
            await Tier2SqlHelpers.InsertInboxAsync(npgsql, messageId, "ConsumerB");

            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                Tier2SqlHelpers.InsertInboxAsync(npgsql, messageId, "ConsumerA")
            );
            Assert.Equal("23505", ex.SqlState);
        });
    }

    [Fact]
    public async Task OutboxMessage_MalformedJsonPayload_RejectedByJsonbConstraint()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, _) =>
        {
            await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                Tier2SqlHelpers.InsertOutboxCastJsonbAsync(
                    npgsql,
                    Guid.CreateVersion7(),
                    "Ruth.Test.Event",
                    "{invalid-json"
                )
            );
            Assert.Equal("22P02", ex.SqlState);
        });
    }

    [Fact]
    public async Task OutboxMessage_EmptyJsonObjectPayload_DefaultsRetryCountToZero()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, probe) =>
        {
            var id = Guid.CreateVersion7();
            await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
            var now = DateTimeOffset.UtcNow;
            await Tier2SqlHelpers.InsertOutboxAsync(npgsql, id, "Ruth.Test.NoOp", "{}", now);
            await Tier2SqlHelpers.AssertOutboxRetryCountZeroAsync(probe, id);
        });
    }

    [Fact]
    public async Task OutboxMessage_OverlengthEventType_ThrowsTruncation22001()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, _) =>
        {
            await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
            var tooLong = new string('E', 600);
            var now = DateTimeOffset.UtcNow;
            var ex = await Assert.ThrowsAsync<PostgresException>(() =>
                Tier2SqlHelpers.InsertOutboxAsync(npgsql, Guid.CreateVersion7(), tooLong, "{}", now)
            );
            Assert.Equal("22001", ex.SqlState);
        });
    }

    [Fact]
    public async Task OutboxAndInbox_SchemaConstraints_VerifyCompositePkAndPartialIndex()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, _, probe) =>
        {
            var pkCols = await probe.GetPrimaryKeyColumnsAsync("identity", "inbox_messages");
            Assert.Equal(_inboxPkColumns, pkCols);

            var indexes = await probe.GetIndexesAsync("identity", "outbox_messages");
            Assert.Contains(indexes, idx =>
                idx.Definition.Contains("WHERE", StringComparison.OrdinalIgnoreCase)
                && idx.Definition.Contains("processed_on_utc", StringComparison.OrdinalIgnoreCase)
            );
        });
    }

    [Fact]
    public async Task OutboxDispatcher_UnknownEventType_RecordsErrorAndDeadLetters()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, client, conn, probe) =>
            {
                var id = Guid.CreateVersion7();
                await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
                const string missing = "Ruth.NonExistent.MissingEvent, MissingAssembly";
                await Tier2SqlHelpers.InsertOutboxAsync(
                    npgsql,
                    id,
                    missing,
                    "{}",
                    DateTimeOffset.UtcNow
                );

                var deadLettered = await Tier2TestHelpers.WaitUntilAsync(
                    () => Tier2SqlHelpers.IsDeadLetteredWithErrorAsync(probe, id),
                    TimeSpan.FromSeconds(15)
                );
                Assert.True(deadLettered);
                await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
            },
            overrides: _fastOutbox
        );
    }

    [Fact]
    public async Task OutboxDispatcher_RepeatedPollingTicks_DoesNotDoubleProcessCompletedRow()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, _, _, probe) =>
            {
                DateTimeOffset? firstTs = null;
                var processed = await Tier2TestHelpers.WaitUntilAsync(
                    async () =>
                    {
                        firstTs = await Tier2SqlHelpers.TryGetFirstProcessedTimestampAsync(probe);
                        return firstTs is not null;
                    },
                    TimeSpan.FromSeconds(15)
                );

                Assert.True(processed);
                await Task.Delay(TimeSpan.FromSeconds(6));
                var nextTs = await Tier2SqlHelpers.TryGetFirstProcessedTimestampAsync(probe);
                Assert.Equal(firstTs, nextTs);
                Assert.Equal(1, await probe.CountIdentityOutboxMessagesAsync());
            },
            overrides: _fastOutbox
        );
    }

    [Fact]
    public async Task OutboxDispatcher_EmptyOutboxTable_PollsGracefullyWithoutErrors()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var f = RuthHostFactory.Create(_containers, conn, overrides: _fastOutbox);
            using var client = f.CreateNoRedirectClient();
            await Task.Delay(TimeSpan.FromSeconds(6));
            await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
            Assert.Equal(0, await probe.CountIdentityOutboxMessagesAsync());
            Assert.Equal(0, await probe.CountIdentityInboxMessagesAsync());
        });
    }

    [Fact]
    public async Task OutboxDispatcher_GracefulHostShutdown_StopsWithoutHanging()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            var key = RuthHostFactory.SafeTestJwtSigningKey;
            var env = Tier2TestHelpers.BuildProcessEnvironment(
                _containers,
                conn,
                "Development",
                key,
                "true"
            );
            env["Outbox__PollingInterval"] = "00:00:00.200";
            await using var host = await HostProcessRunner.StartHostAsync(
                env,
                TimeSpan.FromSeconds(25)
            );
            using var client = host.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(client);
            await Task.Delay(TimeSpan.FromMilliseconds(400));
            Assert.False((await host.StopAndCollectAsync()).TimedOut);
        });
    }

    [Fact]
    public async Task OutboxDispatcher_AlreadyProcessedRow_IsSkippedAcrossTicks()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, _, conn, probe) =>
            {
                var id = Guid.CreateVersion7();
                await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
                var now = DateTimeOffset.UtcNow;
                await Tier2SqlHelpers.InsertOutboxAsync(
                    npgsql,
                    id,
                    "Ruth.NonExistent.AlreadyHandled",
                    "{}",
                    now.AddMinutes(-6),
                    now.AddMinutes(-5)
                );
                await Task.Delay(TimeSpan.FromSeconds(6));
                await Tier2SqlHelpers.AssertOutboxRetryCountZeroAsync(probe, id);
            },
            overrides: _fastOutbox
        );
    }
}
