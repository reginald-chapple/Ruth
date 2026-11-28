using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Persistence.EntityFramework;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class CrossTenantCanaryTestHelper
{
    internal static readonly string[] TenantSchemas =
    [
        "identity",
        "core",
        "crm",
        "groups",
        "checkin",
        "comms",
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

        await AssertAppRoleNonSuperuserNoBypassAsync(conn);
        var tables = await InspectAllTenantTablesForceRlsAsync(conn);
        await AssertPermissiveAndRestrictivePoliciesOnEveryTableAsync(conn, tables);
    }

    public static async Task AssertAppRoleZeroRowsAndPlatformDenyAsync(
        string adminConnectionString,
        IServiceProvider services
    )
    {
        await using var adminConn = new NpgsqlConnection(adminConnectionString);
        await adminConn.OpenAsync();
        var tables = await InspectAllTenantTablesForceRlsAsync(adminConn);

        var provisioner = services.GetRequiredService<PostgresRlsRoleProvisioner>();
        await using var appConn = new NpgsqlConnection(
            provisioner.BuildTenantRuntimeConnectionString(adminConnectionString)
        );
        await appConn.OpenAsync();

        var platformEx = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var denyCmd = appConn.CreateCommand();
            denyCmd.CommandText = "SELECT COUNT(*) FROM platform.tenants";
            _ = await denyCmd.ExecuteScalarAsync();
        });
        Assert.Equal("42501", platformEx.SqlState);

        foreach (var (schema, table) in tables)
        {
            await using var cmd = appConn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM \"{schema}\".\"{table}\"";
            var count = Convert.ToInt64(
                await cmd.ExecuteScalarAsync(),
                CultureInfo.InvariantCulture
            );
            Assert.True(
                count == 0L,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Expected 0 rows under ruth_app with unset GUC on {schema}.{table}, saw {count}."
                )
            );
        }
    }

    public static HttpRequestMessage CreateHostRequest(
        HttpMethod method,
        string host,
        string path,
        object? jsonBody = null,
        string? bearerToken = null,
        string? cookieHeader = null,
        bool useHttps = false
    )
    {
        var scheme = useHttps ? "https" : "http";
        var req = new HttpRequestMessage(
            method,
            new Uri($"{scheme}://{host}{path}", UriKind.Absolute)
        );
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

    private static async Task AssertAppRoleNonSuperuserNoBypassAsync(NpgsqlConnection conn)
    {
        await using var roleCmd = conn.CreateCommand();
        roleCmd.CommandText =
            "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'ruth_app'";
        await using var roleReader = await roleCmd.ExecuteReaderAsync();
        Assert.True(await roleReader.ReadAsync(), "Expected ruth_app role to exist.");
        Assert.False(roleReader.GetBoolean(0), "ruth_app must NOT be superuser.");
        Assert.False(roleReader.GetBoolean(1), "ruth_app must have NOBYPASSRLS.");
    }

    private static async Task<
        List<(string Schema, string Table)>
    > InspectAllTenantTablesForceRlsAsync(NpgsqlConnection conn)
    {
        const string sql =
            "SELECT n.nspname, c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c "
            + "JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = ANY(@schemas) AND c.relkind IN ('r', 'p') "
            + "AND c.relname <> '__EFMigrationsHistory' ORDER BY n.nspname, c.relname";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        _ = cmd.Parameters.AddWithValue("schemas", TenantSchemas);

        List<(string Schema, string Table)> tables = [];
        var seenSchemas = new HashSet<string>(StringComparer.Ordinal);
        List<string> unprotected = [];
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var schema = reader.GetString(0);
            var table = reader.GetString(1);
            tables.Add((schema, table));
            _ = seenSchemas.Add(schema);

            var rls = reader.GetBoolean(2);
            var forceRls = reader.GetBoolean(3);
            if (!rls || !forceRls)
            {
                unprotected.Add($"{schema}.{table} (rls={rls}, force={forceRls})");
            }
        }

        var missingSchemas = TenantSchemas.Except(seenSchemas, StringComparer.Ordinal).ToArray();
        Assert.True(
            missingSchemas.Length == 0,
            $"Missing tenant schemas: {string.Join(", ", missingSchemas)}"
        );
        Assert.True(
            tables.Count >= 150,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected >= 150 tenant tables across 15 schemas, saw {tables.Count}."
            )
        );
        Assert.True(
            unprotected.Count == 0,
            $"Tables missing FORCE ROW LEVEL SECURITY: {string.Join(", ", unprotected)}"
        );
        return tables;
    }

    private static async Task AssertPermissiveAndRestrictivePoliciesOnEveryTableAsync(
        NpgsqlConnection conn,
        List<(string Schema, string Table)> tables
    )
    {
        const string policySql =
            "SELECT schemaname || '.' || tablename, permissive, COALESCE(qual, '') "
            + "FROM pg_policies WHERE schemaname = ANY(@schemas)";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = policySql;
        _ = cmd.Parameters.AddWithValue("schemas", TenantSchemas);

        var byTable = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var key = reader.GetString(0);
            var permissive = reader.GetString(1);
            var qual = reader.GetString(2);
            Assert.Contains("app.current_tenant_id", qual, StringComparison.Ordinal);

            if (!byTable.TryGetValue(key, out var kinds))
            {
                kinds = new HashSet<string>(StringComparer.Ordinal);
                byTable[key] = kinds;
            }

            _ = kinds.Add(permissive);
        }

        foreach (var (schema, table) in tables)
        {
            var fq = $"{schema}.{table}";
            Assert.True(
                byTable.TryGetValue(fq, out var kinds)
                    && kinds.Contains("PERMISSIVE")
                    && kinds.Contains("RESTRICTIVE"),
                $"Expected both PERMISSIVE and RESTRICTIVE RLS policies on {fq}."
            );
        }
    }
}
