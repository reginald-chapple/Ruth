using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class CrossTenantCanaryTestHelper
{
    private static readonly string[] _tenantSchemas =
    [
        "identity",
        "core",
        "crm",
        "groups",
        "checkin",
        "comm",
        "finance",
        "events",
        "commerce",
        "engage",
        "workflow",
        "cms",
        "reporting",
        "lms",
        "search",
    ];

    public static HostWebApplicationFactory CreateMultiTenantSaasFactory(
        HostContainersFixture containers,
        string isolatedDbConnection
    )
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
            ["Tenancy:DedicatedWorkspaceTenantId"] = string.Empty,
            ["Tenancy:SandboxBaseDomain"] = "sandbox.localhost",
            ["Tenancy:PlatformOperatorHost"] = "ops.localhost",
            ["Tenancy:SeedOperatorEmail"] = "compliance@ops.localhost",
            ["Tenancy:SeedOperatorPassword"] = "OperatorPass!2026#",
            ["Tenancy:StripeMockMode"] = "true",
        };

        return HostWebApplicationFactory.Create(
            isolatedDbConnection,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: overrides
        );
    }

    public static async Task AssertPostgresForceRlsOnAllTenantTablesAsync(
        string adminConnectionString
    )
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();

        await using (var roleCmd = conn.CreateCommand())
        {
            roleCmd.CommandText =
                "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'ruth_app'";
            await using var roleReader = await roleCmd.ExecuteReaderAsync();
            Assert.True(await roleReader.ReadAsync(), "Expected ruth_app role to exist.");
            Assert.False(roleReader.GetBoolean(0), "ruth_app must NOT be superuser.");
            Assert.False(roleReader.GetBoolean(1), "ruth_app must have NOBYPASSRLS.");
        }

        const string sql =
            "SELECT n.nspname || '.' || c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c "
            + "JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = ANY(@schemas) AND c.relkind IN ('r', 'p') "
            + "AND c.relname <> '__EFMigrationsHistory'";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        _ = cmd.Parameters.AddWithValue("schemas", _tenantSchemas);

        var inspectedCount = 0;
        List<string> unprotected = [];
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            inspectedCount++;
            var table = reader.GetString(0);
            var rls = reader.GetBoolean(1);
            var forceRls = reader.GetBoolean(2);
            if (!rls || !forceRls)
            {
                unprotected.Add($"{table} (rls={rls}, force={forceRls})");
            }
        }

        Assert.True(
            inspectedCount >= 80,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected >= 80 tenant tables, saw {inspectedCount}."
            )
        );
        Assert.True(
            unprotected.Count == 0,
            $"Tables missing FORCE ROW LEVEL SECURITY: {string.Join(", ", unprotected)}"
        );
    }

    public static HttpRequestMessage CreateHostRequest(
        HttpMethod method,
        string host,
        string path,
        object? jsonBody = null,
        string? bearerToken = null,
        string? cookieHeader = null
    )
    {
        var req = new HttpRequestMessage(method, new Uri($"http://{host}{path}", UriKind.Absolute));
        req.Headers.Host = host;
        if (jsonBody is not null)
        {
            req.Content = JsonContent.Create(jsonBody);
        }

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            req.Headers.Add("Cookie", cookieHeader);
        }

        return req;
    }

    public static async Task<(Guid TenantId, string SandboxHost)> ProvisionTaxExemptTenantAsync(
        HttpClient client,
        string legalName,
        string displayName,
        string slug,
        string ein,
        string adminEmail,
        string password
    )
    {
        using var req = CreateHostRequest(
            HttpMethod.Post,
            "signup.localhost",
            "/api/v1/platform/signup",
            new
            {
                legalName,
                displayName,
                slug,
                ein,
                irsSubsectionCode = "501(c)(3)",
                stateCode = "MD",
                timeZoneId = "America/New_York",
                primaryBranchName = "Main Center",
                adminUserName = "orgadmin",
                adminEmail,
                adminDisplayName = $"{displayName} Director",
                adminPassword = password,
            }
        );
        using var resp = await client.SendAsync(req);
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.True(
            resp.StatusCode == System.Net.HttpStatusCode.Created,
            $"Signup failed ({resp.StatusCode}): {raw}"
        );
        using var doc = JsonDocument.Parse(raw);
        return (
            doc.RootElement.GetProperty("tenantId").GetGuid(),
            doc.RootElement.GetProperty("sandboxHostname").GetString()!
        );
    }

    public static async Task<(string AccessToken, string SidCookie)> LoginTenantAdminAsync(
        HttpClient client,
        string host,
        string password
    )
    {
        using var jwtReq = CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/auth/login",
            new { userNameOrEmail = "orgadmin", password }
        );
        using var jwtResp = await client.SendAsync(jwtReq);
        Assert.Equal(System.Net.HttpStatusCode.OK, jwtResp.StatusCode);
        using var jwtDoc = JsonDocument.Parse(await jwtResp.Content.ReadAsStringAsync());
        var token = jwtDoc.RootElement.GetProperty("accessToken").GetString()!;

        using var cookieReq = CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/auth/cookie-login",
            new
            {
                userNameOrEmail = "orgadmin",
                password,
                rememberMe = false,
            }
        );
        using var cookieResp = await client.SendAsync(cookieReq);
        Assert.Equal(System.Net.HttpStatusCode.OK, cookieResp.StatusCode);
        Assert.True(cookieResp.Headers.TryGetValues("Set-Cookie", out var cookies));
        var rawSetCookie = cookies.First(static c =>
            c.StartsWith("sid=", StringComparison.Ordinal)
        );
        Assert.DoesNotContain("domain=", rawSetCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ruth", rawSetCookie, StringComparison.OrdinalIgnoreCase);

        return (token, rawSetCookie.Split(';')[0]);
    }
}
