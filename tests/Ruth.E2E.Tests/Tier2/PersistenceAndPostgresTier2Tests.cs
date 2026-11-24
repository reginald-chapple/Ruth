using System.Net;
using Npgsql;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class PersistenceAndPostgresTier2Tests(E2EContainersFixture containers)
{
    private static readonly string[] _forbiddenPublicTables =
    [
        "UserAccounts",
        "user_accounts",
        "OrganizationProfiles",
        "organization_profiles",
        "OutboxMessages",
        "outbox_messages",
        "ScheduledJobs",
        "scheduled_jobs",
    ];

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Postgres_UnreachableDatabasePort_FailsHostBootOrReadinessCleanly()
    {
        var port = Tier2TestHelpers.GetUnusedLoopbackPort();
        var deadConn = Tier2HttpHelpers.BuildDeadPostgresConn(port);
        await using var factory = RuthHostFactory.CreateWithCustomEndpoints(
            deadConn,
            _containers.RedisConnectionString,
            _containers.RustFsEndpoint
        );

        try
        {
            using var client = factory.CreateNoRedirectClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var resp = await Tier2HttpHelpers.GetRelativeAsync(client, "/health", cts.Token);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, resp.StatusCode);
        }
        catch (Exception ex)
            when (ex is NpgsqlException or HttpRequestException or TimeoutException)
        {
            Assert.NotNull(ex.Message);
        }
    }

    [Fact]
    public async Task Postgres_InvalidConnectionStringSyntax_FailsFastCleanly()
    {
        var env = Tier2TestHelpers.BuildProcessEnvironment(
            _containers,
            "invalid-connection-string-without-equals",
            "Development",
            RuthHostFactory.SafeTestJwtSigningKey,
            bootstrapAutoMigrate: "true"
        );

        var result = await HostProcessRunner.RunHostAsync(env, TimeSpan.FromSeconds(20));
        Assert.False(result.TimedOut, "Host should fail fast on invalid connection string.");
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task Postgres_IsolatedDatabases_DoNotLeakOnboardedStateBetweenRuns()
    {
        var dbA = await _containers.CreateIsolatedDatabaseAsync();
        var dbB = await _containers.CreateIsolatedDatabaseAsync();
        try
        {
            await using (var factoryA = RuthHostFactory.Create(_containers, dbA))
            using (var clientA = factoryA.CreateNoRedirectClient())
            {
                await Tier2TestHelpers.CompleteOnboardingAsync(clientA);
            }

            await using var factoryB = RuthHostFactory.Create(_containers, dbB);
            using var clientB = factoryB.CreateNoRedirectClient();
            using var rootResp = await Tier2HttpHelpers.GetRelativeAsync(clientB, "/");

            Assert.Equal(HttpStatusCode.Redirect, rootResp.StatusCode);
            var location = rootResp.Headers.Location?.ToString() ?? string.Empty;
            Assert.Contains("/onboarding", location, StringComparison.OrdinalIgnoreCase);
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(
                new DatabaseProbe(_containers.GetConnectionString(dbB))
            );
        }
        finally
        {
            await _containers.DropDatabaseAsync(dbB);
            await _containers.DropDatabaseAsync(dbA);
        }
    }

    [Fact]
    public async Task Postgres_SchemaIsolation_DomainTablesNotPlacedInPublicSchema()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, _, probe) =>
        {
            Assert.True(await probe.SchemaExistsAsync("identity"));
            Assert.True(await probe.SchemaExistsAsync("scheduler"));
            var publicTables = await probe.GetTablesInSchemaAsync("public");
            foreach (var forbidden in _forbiddenPublicTables)
            {
                Assert.DoesNotContain(forbidden, publicTables, StringComparer.OrdinalIgnoreCase);
            }
        });
    }

    [Fact]
    public async Task Postgres_DatabaseProbe_NonExistentTableReturnsFalseAndThrows()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, _, probe) =>
        {
            Assert.False(await probe.TableExistsAsync("identity", "nonexistent_table_xyz"));
            _ = await Assert.ThrowsAsync<NotSupportedException>(() =>
                probe.CountRowsAsync("identity", "nonexistent_table_xyz")
            );
        });
    }

    [Fact]
    public async Task AuditInterceptor_UserAndOrgTimestamps_AreNonNullUtcAndInWindow()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            var minUtc = DateTimeOffset.UtcNow.AddSeconds(-5);
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(client);
            var maxUtc = DateTimeOffset.UtcNow.AddSeconds(5);

            var userRow = Assert.Single(await probe.ReadUserAccountsAsync());
            var orgRow = Assert.Single(await probe.ReadOrganizationProfilesAsync());
            Tier2SqlHelpers.AssertCreatedOnUtcInRange(userRow, minUtc, maxUtc);
            Tier2SqlHelpers.AssertCreatedOnUtcInRange(orgRow, minUtc, maxUtc);
        });
    }

    [Fact]
    public async Task AuditInterceptor_AuditColumns_UseTimestamptzAndNotNullCreatedOnUtc()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, _, probe) =>
        {
            var userTable = await probe.ResolveFirstExistingTableAsync(
                "identity",
                Tier2TestHelpers.UserAccountTables
            );
            Assert.NotNull(userTable);
            var columns = await probe.GetColumnsAsync("identity", userTable);
            var createdCol = columns.Single(c =>
                c.ColumnName.Replace("_", string.Empty, StringComparison.Ordinal)
                    .Equals("CreatedOnUtc", StringComparison.OrdinalIgnoreCase)
            );

            Assert.Equal("timestamptz", createdCol.UdtName);
            Assert.False(createdCol.IsNullable);
        });
    }

    [Fact]
    public async Task ConcurrencyAndUniqueness_DuplicateUserAccountPk_Throws23505()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, _, conn, probe) =>
        {
            var userRow = Assert.Single(await probe.ReadUserAccountsAsync());
            Assert.True(DatabaseProbe.TryGetColumnValue(userRow, "Id", out var idVal));
            var userTable = await probe.ResolveFirstExistingTableAsync(
                "identity",
                Tier2TestHelpers.UserAccountTables
            );
            await using var npgsql = await Tier2SqlHelpers.OpenConnectionAsync(conn);
            var userId = Assert.IsType<Guid>(idVal);
            await using var cmd = Tier2SqlHelpers.CreateDuplicateUserInsertCommand(
                npgsql,
                userTable!,
                userId
            );
            var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
            Assert.Equal("23505", ex.SqlState);
        });
    }

    [Fact]
    public async Task Concurrency_ParallelFirstRunOnboarding_AllowsExactlyOneWinner()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var c1 = factory.CreateNoRedirectClient();
            using var c2 = factory.CreateNoRedirectClient();
            var res = await Task.WhenAll(
                Tier2TestHelpers.PostOnboardingAsync(c1),
                Tier2TestHelpers.PostOnboardingAsync(c2)
            );
            var ok = res.Count(r => r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);
            var dup = res.Count(r =>
                r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden
            );
            Array.ForEach(res, r => r.Dispose());
            Assert.Equal(1, ok);
            Assert.Equal(1, dup);
            Assert.Equal(1, await probe.CountUserAccountsAsync());
            Assert.Equal(1, await probe.CountOrganizationProfilesAsync());
        });
    }

    [Fact]
    public async Task AtomicSaveChanges_FailedOnboardingValidation_WritesZeroOutboxRows()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var badPayload = TestOnboardingPayloads.CreateInvalidTimeZonePayload();
            using var resp = await Tier2TestHelpers.PostOnboardingAsync(client, badPayload);
            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(resp, HttpStatusCode.BadRequest);
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);
            Assert.Equal(0, await probe.CountIdentityOutboxMessagesAsync());
        });
    }
}
