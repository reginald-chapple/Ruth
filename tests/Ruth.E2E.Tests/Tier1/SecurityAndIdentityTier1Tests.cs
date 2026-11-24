using System.Globalization;
using System.Net;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class SecurityAndIdentityTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _securityRoleCandidates = ["SecurityRoles", "security_roles"];
    private static readonly string[] _userRoleCandidates = ["UserRoles", "user_roles"];
    private static readonly string[] _userRoleUserIdColumns =
    [
        "UserAccountId",
        "user_account_id",
        "UserId",
        "user_id",
    ];
    private static readonly string[] _orgNameColumns =
    [
        "Name",
        "name",
        "OrganizationName",
        "organization_name",
    ];
    private static readonly string[] _timeZoneColumns = ["TimeZoneId", "time_zone_id"];
    private static readonly string[] _userNameColumns = ["UserName", "user_name"];
    private static readonly string[] _emailColumns = ["Email", "email"];
    private static readonly string[] _passwordHashColumns = ["PasswordHash", "password_hash"];

    [Fact]
    public async Task Feature13_ProductionHost_StartsWith32CharSigningKey()
    {
        const string key32 = "12345678901234567890123456789012";
        await AssertHostProcessStartsAsync("Production", key32, "/health/live");
    }

    [Fact]
    public async Task Feature13_ProductionHost_StartsWith64CharSigningKey()
    {
        const string key64 = "1234567890123456789012345678901234567890123456789012345678901234";
        await AssertHostProcessStartsAsync("Production", key64, "/health/live");
    }

    [Fact]
    public async Task Feature13_DevelopmentHost_AllowsDefaultDevSigningKey()
    {
        await AssertHostProcessStartsAsync(
            "Development",
            RuthHostFactory.DefaultDevJwtSigningKey,
            "/health/live"
        );
    }

    [Fact]
    public async Task Feature13_ProductionHost_ServesReadinessProbeWithValidKey()
    {
        await AssertHostProcessStartsAsync(
            "Production",
            RuthHostFactory.SafeTestJwtSigningKey,
            "/health"
        );
    }

    [Fact]
    public async Task Feature13_ProductionHost_ShutsDownCleanlyAfterValidStartup()
    {
        var dbName = await containers.CreateIsolatedDatabaseAsync();
        try
        {
            var connectionString = containers.GetConnectionString(dbName);
            var env = Tier1TestSupport.BuildProcessEnvironment(
                containers,
                connectionString,
                "Production",
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

    [Fact]
    public async Task Feature14_IdentitySchema_ContainsRequiredWave0Tables()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);

        var userTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.UserAccountTableCandidates
        );
        var orgTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            ProbeSqlCatalog.OrganizationTableCandidates
        );
        var roleTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            _securityRoleCandidates
        );
        var userRoleTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            _userRoleCandidates
        );

        Assert.NotNull(userTable);
        Assert.NotNull(orgTable);
        Assert.NotNull(roleTable);
        Assert.NotNull(userRoleTable);
    }

    [Fact]
    public async Task Feature14_Onboarding_PersistsOrganizationProfileWithTimeZoneId()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var payload = TestOnboardingPayloads.CreateValidPayload(
            organizationName: "Heartland Fellowship",
            timeZoneId: "America/Chicago"
        );
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client, payload);
        Assert.NotNull(onboardingDoc);

        var orgRow = Assert.Single(await scope.Probe.ReadOrganizationProfilesAsync());
        Assert.True(Tier1TestSupport.TryGetAnyColumn(orgRow, _orgNameColumns, out var nameRaw));
        Assert.True(Tier1TestSupport.TryGetAnyColumn(orgRow, _timeZoneColumns, out var tzRaw));

        Assert.Equal(
            "Heartland Fellowship",
            Convert.ToString(nameRaw, CultureInfo.InvariantCulture)
        );
        Assert.Equal("America/Chicago", Convert.ToString(tzRaw, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Feature14_Onboarding_PersistsAdminUserAccountAndSecurityRoleLink()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var payload = TestOnboardingPayloads.CreateValidPayload(
            adminUserName: "ruth.admin",
            adminEmail: "ruth.admin@gracechurch.test"
        );
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client, payload);
        Assert.NotNull(onboardingDoc);

        var userRow = Assert.Single(await scope.Probe.ReadUserAccountsAsync());
        Assert.True(Tier1TestSupport.TryGetAnyColumn(userRow, _userNameColumns, out var userRaw));
        Assert.True(Tier1TestSupport.TryGetAnyColumn(userRow, _emailColumns, out var emailRaw));
        Assert.Equal("ruth.admin", Convert.ToString(userRaw, CultureInfo.InvariantCulture));
        Assert.Equal(
            "ruth.admin@gracechurch.test",
            Convert.ToString(emailRaw, CultureInfo.InvariantCulture)
        );

        var roleTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            _securityRoleCandidates
        );
        var userRoleTable = await scope.Probe.ResolveFirstExistingTableAsync(
            "identity",
            _userRoleCandidates
        );
        Assert.NotNull(roleTable);
        Assert.NotNull(userRoleTable);

        var adminUserId = onboardingDoc.RootElement.GetProperty("adminUserId").GetGuid();
        Assert.True(await scope.Probe.CountRowsAsync("identity", roleTable) >= 1);

        var userRoleRows = await scope.Probe.ReadRowsAsync("identity", userRoleTable);
        Assert.Contains(userRoleRows, row =>
            Tier1TestSupport.TryGetAnyColumn(row, _userRoleUserIdColumns, out var linkedRaw)
            && linkedRaw is Guid linkedUserId
            && linkedUserId == adminUserId
        );
    }

    [Fact]
    public async Task Feature14_UserAccount_StoresNonPlaintextPasswordHash()
    {
        const string rawPassword = "Str0ng!Onboarding#2026";
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var payload = TestOnboardingPayloads.CreateValidPayload(adminPassword: rawPassword);
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client, payload);
        Assert.NotNull(onboardingDoc);

        var userRow = Assert.Single(await scope.Probe.ReadUserAccountsAsync());
        Assert.True(
            Tier1TestSupport.TryGetAnyColumn(userRow, _passwordHashColumns, out var hashRaw)
        );

        var hash = Convert.ToString(hashRaw, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(rawPassword, hash, StringComparer.Ordinal);
        Assert.DoesNotContain(rawPassword, hash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Feature14_HttpResponses_NeverLeakPasswordHashOrPlaintextPassword()
    {
        const string rawPassword = "Str0ng!Onboarding#2026";
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var payload = TestOnboardingPayloads.CreateValidPayload(adminPassword: rawPassword);
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client, payload);

        var onboardingJson = onboardingDoc.RootElement.GetRawText();
        var homeHtml = await client.GetStringAsync(new Uri("/", UriKind.Relative));

        Assert.DoesNotContain("passwordHash", onboardingJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawPassword, onboardingJson, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", homeHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawPassword, homeHtml, StringComparison.Ordinal);
    }

    private async Task AssertHostProcessStartsAsync(
        string environment,
        string signingKey,
        string probePath
    )
    {
        var dbName = await containers.CreateIsolatedDatabaseAsync();
        try
        {
            var connectionString = containers.GetConnectionString(dbName);
            var env = Tier1TestSupport.BuildProcessEnvironment(
                containers,
                connectionString,
                environment,
                signingKey
            );

            await using var host = await HostProcessRunner.StartHostAsync(
                env,
                TimeSpan.FromSeconds(30)
            );
            using var response = await host.GetAsync(probePath);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            await containers.DropDatabaseAsync(dbName);
        }
    }
}
