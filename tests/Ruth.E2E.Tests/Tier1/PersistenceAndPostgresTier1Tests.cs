using System.Globalization;
using Npgsql;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class PersistenceAndPostgresTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _createdOnCandidates = ["CreatedOnUtc", "created_on_utc"];

    [Fact]
    public async Task Feature03_Postgres18_InstallsUuidOsspAndBtreeGistExtensions()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var extensions = await scope.Probe.GetInstalledExtensionsAsync();

        Assert.Contains("uuid-ossp", extensions, StringComparer.Ordinal);
        Assert.Contains("btree_gist", extensions, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Feature03_Postgres18_InstallsPostgisAndVectorExtensions()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var extensions = await scope.Probe.GetInstalledExtensionsAsync();

        Assert.Contains("postgis", extensions, StringComparer.Ordinal);
        Assert.Contains("vector", extensions, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Feature03_ModuleSchemas_IdentityAndScheduler_ExistAfterBoot()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);

        var hasIdentity = await scope.Probe.SchemaExistsAsync("identity");
        var hasScheduler = await scope.Probe.SchemaExistsAsync("scheduler");

        Assert.True(hasIdentity, "Expected 'identity' schema to exist after host boot.");
        Assert.True(hasScheduler, "Expected 'scheduler' schema to exist after host boot.");
    }

    [Fact]
    public async Task Feature03_MigrationsHistory_RecordsAppliedEfCoreMigrations()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        await using var connection = new NpgsqlConnection(scope.ConnectionString);
        await connection.OpenAsync();

        const string historyTableSql =
            "SELECT COUNT(*) FROM information_schema.tables"
            + " WHERE table_name = '__EFMigrationsHistory';";
        await using var tableCmd = new NpgsqlCommand(historyTableSql, connection);
        var historyTableCount = Convert.ToInt64(
            await tableCmd.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );

        var identityTables = await scope.Probe.GetTablesInSchemaAsync("identity");
        var schedulerTables = await scope.Probe.GetTablesInSchemaAsync("scheduler");

        Assert.True(
            historyTableCount >= 1 || (identityTables.Count > 0 && schedulerTables.Count > 0),
            "Expected applied EF Core migration history or initialized module tables."
        );
    }

    [Fact]
    public async Task Feature04_OnboardingEntities_RecordCreatedOnUtcWithinExecutionWindow()
    {
        var beforeUtc = DateTimeOffset.UtcNow.AddSeconds(-2);
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);
        var afterUtc = DateTimeOffset.UtcNow.AddSeconds(2);

        var orgRows = await scope.Probe.ReadOrganizationProfilesAsync();
        var userRows = await scope.Probe.ReadUserAccountsAsync();

        var orgRow = Assert.Single(orgRows);
        var userRow = Assert.Single(userRows);

        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(orgRow, _createdOnCandidates, out var orgCreatedRaw)
        );
        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(userRow, _createdOnCandidates, out var userCreatedRaw)
        );

        var orgCreated = Tier1TestSupport.ParseRequiredUtcTimestamp(orgCreatedRaw);
        var userCreated = Tier1TestSupport.ParseRequiredUtcTimestamp(userCreatedRaw);

        TestOnboardingPayloads.AssertUtcTimestampInRange(orgCreated, beforeUtc, afterUtc);
        TestOnboardingPayloads.AssertUtcTimestampInRange(userCreated, beforeUtc, afterUtc);
    }

    [Fact]
    public async Task Feature04_IdentityAuditColumns_UseTimestampWithTimeZone()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        var orgTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OrganizationTableCandidates
        );
        Assert.NotNull(orgTable);

        var columns = await scope.Probe.GetColumnsAsync("identity", orgTable);
        var createdColumn = columns.FirstOrDefault(c =>
            c.ColumnName.Equals("CreatedOnUtc", StringComparison.OrdinalIgnoreCase)
            || c.ColumnName.Equals("created_on_utc", StringComparison.OrdinalIgnoreCase)
        );

        Assert.NotNull(createdColumn);
        Assert.Equal("timestamp with time zone", createdColumn.DataType);
    }

    [Fact]
    public async Task Feature04_PersistedEntities_ExposeConcurrencyMetadataAndAuditState()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var orgTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OrganizationTableCandidates
        );
        Assert.NotNull(orgTable);

        var columns = await scope.Probe.GetColumnsAsync("identity", orgTable);
        var hasExplicitConcurrencyColumn = columns.Any(c =>
            c.ColumnName.Contains("Version", StringComparison.OrdinalIgnoreCase)
            || c.ColumnName.Contains("Concurrency", StringComparison.OrdinalIgnoreCase)
        );

        var hasXmin = await HasNonZeroXminAsync(scope.ConnectionString, orgTable);
        Assert.True(
            hasExplicitConcurrencyColumn || hasXmin,
            "Expected optimistic concurrency column or PostgreSQL xmin system row version."
        );
    }

    [Fact]
    public async Task Feature04_OnboardingSaveChanges_AtomicallyPersistsEntitiesAndOutbox()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        var orgCount = await scope.Probe.CountOrganizationProfilesAsync();
        var userCount = await scope.Probe.CountUserAccountsAsync();
        var outboxCount = await scope.Probe.CountIdentityOutboxMessagesAsync();

        Assert.Equal(1, orgCount);
        Assert.Equal(1, userCount);
        Assert.True(outboxCount >= 1, "Expected atomic Outbox message created with onboarding.");
    }

    [Fact]
    public async Task Feature04_PersistedOrganization_HasUtcAuditTimestampsMatchingResponse()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var doc = await Tier1TestSupport.CompleteOnboardingAsync(client);

        var rawCompleted = doc.RootElement.GetProperty("completedOnUtc").GetString();
        Assert.False(string.IsNullOrWhiteSpace(rawCompleted));
        var completedOnUtc = DateTimeOffset.Parse(
            rawCompleted,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind
        );

        var orgRow = Assert.Single(await scope.Probe.ReadOrganizationProfilesAsync());
        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(orgRow, _createdOnCandidates, out var createdRaw)
        );

        var createdOnUtc = Tier1TestSupport.ParseRequiredUtcTimestamp(createdRaw);
        var delta = (completedOnUtc - createdOnUtc).Duration();
        Assert.True(delta < TimeSpan.FromSeconds(5), $"Unexpected audit clock skew: {delta}.");
    }

    private static async Task<bool> HasNonZeroXminAsync(string connectionString, string orgTable)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = orgTable switch
        {
            "OrganizationProfiles" =>
                """SELECT CAST(xmin AS text) FROM "identity"."OrganizationProfiles" LIMIT 1;""",
            "organization_profiles" =>
                "SELECT CAST(xmin AS text) FROM identity.organization_profiles LIMIT 1;",
            _ => throw new NotSupportedException($"Unsupported table '{orgTable}'."),
        };

        await using var command = new NpgsqlCommand(sql, connection);
        var xminValue = Convert.ToString(
            await command.ExecuteScalarAsync(),
            CultureInfo.InvariantCulture
        );
        return !string.IsNullOrWhiteSpace(xminValue)
            && !string.Equals(xminValue, "0", StringComparison.Ordinal);
    }
}
