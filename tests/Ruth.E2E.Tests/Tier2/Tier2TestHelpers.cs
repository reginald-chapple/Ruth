using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

internal static class Tier2TestHelpers
{
    public static readonly string[] UserAccountTables = ["UserAccounts", "user_accounts"];

    public static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ruth.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate repository root containing Ruth.slnx."
        );
    }

    public static int GetUnusedLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public static async Task RunWithIsolatedDatabaseAsync(
        E2EContainersFixture containers,
        Func<string, string, DatabaseProbe, Task> action
    )
    {
        var dbName = await containers.CreateIsolatedDatabaseAsync();
        var connectionString = containers.GetConnectionString(dbName);
        var probe = new DatabaseProbe(connectionString);
        try
        {
            await action(dbName, connectionString, probe);
        }
        finally
        {
            await containers.DropDatabaseAsync(dbName);
        }
    }

    public static async Task RunOnboardedAsync(
        E2EContainersFixture containers,
        Func<RuthHostFactory, HttpClient, string, DatabaseProbe, Task> action,
        string environment = "Development",
        IReadOnlyDictionary<string, string?>? overrides = null
    )
    {
        await RunWithIsolatedDatabaseAsync(containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(
                containers,
                conn,
                environment,
                overrides: overrides
            );
            using var client = factory.CreateNoRedirectClient();
            await CompleteOnboardingAsync(client);
            await action(factory, client, conn, probe);
        });
    }

    public static async Task<HttpResponseMessage> PostOnboardingJsonAsync(
        HttpClient client,
        string rawJson
    )
    {
        using var content = new StringContent(rawJson, Encoding.UTF8, "application/json");
        return await client.PostAsync(new Uri("/api/onboarding", UriKind.Relative), content);
    }

    public static async Task<HttpResponseMessage> PostOnboardingAsync(
        HttpClient client,
        JsonObject? payload = null
    )
    {
        using var content = TestOnboardingPayloads.ToJsonContent(
            payload ?? TestOnboardingPayloads.CreateValidPayload()
        );
        return await client.PostAsync(new Uri("/api/onboarding", UriKind.Relative), content);
    }

    public static async Task CompleteOnboardingAsync(HttpClient client, JsonObject? payload = null)
    {
        using var response = await PostOnboardingAsync(client, payload);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected onboarding success (200/201), got {(int)response.StatusCode}: {body}"
            )
        );
    }

    public static async Task<string> AssertProblemDetailsAsync(
        HttpResponseMessage response,
        params HttpStatusCode[] allowedStatuses
    )
    {
        Assert.Contains(response.StatusCode, allowedStatuses);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("json", mediaType, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body), "Expected non-empty ProblemDetails body.");
        AssertNoStackTraceLeak(body);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(
            root.TryGetProperty("status", out var statusProp)
                && statusProp.ValueKind == JsonValueKind.Number
                && statusProp.GetInt32() == (int)response.StatusCode,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected ProblemDetails 'status' {(int)response.StatusCode}, got: {body}"
            )
        );
        Assert.True(
            root.TryGetProperty("title", out var titleProp)
                && titleProp.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(titleProp.GetString()),
            $"Expected non-empty ProblemDetails 'title', got body: {body}"
        );

        return body;
    }

    public static void AssertNoStackTraceLeak(string body)
    {
        Assert.DoesNotContain("at Ruth.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.NullReferenceException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("System.InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql.PostgresException", body, StringComparison.Ordinal);
    }

    public static DateTimeOffset ConvertUtcTimestamp(object? raw)
    {
        Assert.NotNull(raw);
        return raw switch
        {
            DateTime dt when dt.Kind == DateTimeKind.Utc => new DateTimeOffset(dt, TimeSpan.Zero),
            DateTimeOffset dto => dto.ToUniversalTime(),
            _ => throw new InvalidOperationException($"Unexpected timestamp type: {raw.GetType()}"),
        };
    }

    public static async Task<bool> WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null
    )
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(150);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(interval);
        }

        return await condition();
    }

    public static Dictionary<string, string?> BuildProcessEnvironment(
        E2EContainersFixture containers,
        string postgresConnectionString,
        string environment,
        string? signingKey,
        string bootstrapAutoMigrate = "false"
    )
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["ConnectionStrings__RuthDatabase"] = postgresConnectionString,
            ["ConnectionStrings__Redis"] = containers.RedisConnectionString,
            ["ConnectionStrings__RustFs"] = containers.RustFsEndpoint,
            ["Storage__RustFs__Endpoint"] = containers.RustFsEndpoint,
            ["Storage__RustFs__BucketName"] = "ruth-e2e",
            ["Storage__RustFs__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
            ["Storage__RustFs__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
            ["Storage__RustFs__HealthCheckTimeoutSeconds"] = "2",
            ["HealthChecks__TimeoutSeconds"] = "2",
            ["Storage__Endpoint"] = containers.RustFsEndpoint,
            ["Storage__ServiceUrl"] = containers.RustFsEndpoint,
            ["Storage__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
            ["Storage__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
            ["Storage__BucketName"] = "ruth-e2e",
            ["Storage__UseSsl"] = "false",
            ["RustFs__Endpoint"] = containers.RustFsEndpoint,
            ["RustFs__AccessKey"] = E2EContainersFixture.RustFsAccessKey,
            ["RustFs__SecretKey"] = E2EContainersFixture.RustFsSecretKey,
            ["Bootstrap__AutoMigrateAndSeed"] = bootstrapAutoMigrate,
            ["Authentication__Jwt__SigningKey"] = signingKey,
        };
    }

    public static async Task AssertZeroIdentityRowsAsync(DatabaseProbe probe)
    {
        var users = await probe.CountUserAccountsAsync();
        var orgs = await probe.CountOrganizationProfilesAsync();
        Assert.Equal(0, users);
        Assert.Equal(0, orgs);
    }
}
