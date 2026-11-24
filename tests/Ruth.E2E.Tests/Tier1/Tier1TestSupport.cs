using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

internal static class Tier1TestSupport
{
    public const string OnboardingEndpoint = "/api/onboarding";

    public static async Task<IsolatedHostScope> CreateIsolatedHostAsync(
        E2EContainersFixture containers,
        string environment = "Development",
        string? jwtSigningKey = null,
        IReadOnlyDictionary<string, string?>? overrides = null
    )
    {
        var dbName = await containers.CreateIsolatedDatabaseAsync();
        RuthHostFactory? factory = null;
        try
        {
            var connectionString = containers.GetConnectionString(dbName);
            factory = RuthHostFactory.Create(
                containers,
                connectionString,
                environment,
                jwtSigningKey,
                overrides
            );
            _ = factory.Services;
            return new IsolatedHostScope(containers, dbName, connectionString, factory);
        }
        catch
        {
            if (factory is not null)
            {
                await factory.DisposeAsync();
            }

            await containers.DropDatabaseAsync(dbName);
            throw;
        }
    }

    public static async Task<JsonDocument> CompleteOnboardingAsync(
        HttpClient client,
        JsonObject? payload = null
    )
    {
        var body = payload ?? TestOnboardingPayloads.CreateValidPayload();
        using var content = TestOnboardingPayloads.ToJsonContent(body);
        using var response = await client.PostAsync(
            new Uri(OnboardingEndpoint, UriKind.Relative),
            content
        );

        var responseText = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected 200/201 from POST /api/onboarding, got {0}: {1}",
                (int)response.StatusCode,
                responseText
            )
        );

        return JsonDocument.Parse(responseText);
    }

    public static IReadOnlyDictionary<string, string?> BuildProcessEnvironment(
        E2EContainersFixture containers,
        string connectionString,
        string environment,
        string? signingKey
    )
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["ConnectionStrings__RuthDatabase"] = connectionString,
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
            ["Authentication__Jwt__SigningKey"] = signingKey,
            ["Bootstrap__AutoMigrateAndSeed"] = "true",
        };
    }

    public static DateTimeOffset ParseRequiredUtcTimestamp(object? rawValue)
    {
        Assert.NotNull(rawValue);
        if (rawValue is DateTimeOffset dto)
        {
            return dto.ToUniversalTime();
        }

        if (rawValue is DateTime dt)
        {
            var utc =
                dt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    : dt.ToUniversalTime();
            return new DateTimeOffset(utc, TimeSpan.Zero);
        }

        var text = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(text));
        return DateTimeOffset.Parse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
        );
    }

    public static bool TryGetAnyColumn(
        IReadOnlyDictionary<string, object?> row,
        IReadOnlyList<string> candidates,
        out object? value
    )
    {
        foreach (var candidate in candidates)
        {
            if (DatabaseProbe.TryGetColumnValue(row, candidate, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    public static string? ExtractFirstAssetPath(string html, string fragment)
    {
        var index = html.IndexOf(fragment, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var startQuote = html.LastIndexOfAny(['"', '\''], index);
        if (startQuote < 0)
        {
            return null;
        }

        var quoteChar = html[startQuote];
        var endQuote = html.IndexOf(quoteChar, index);
        if (endQuote <= startQuote)
        {
            return null;
        }

        var rawPath = html[(startQuote + 1)..endQuote];
        return rawPath.StartsWith('/') ? rawPath : $"/{rawPath}";
    }

    public static async Task<IReadOnlyDictionary<string, object?>?> WaitForProcessedOutboxRowAsync(
        DatabaseProbe probe,
        IReadOnlyList<string> processedColumns,
        TimeSpan timeout
    )
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var rows = await probe.ReadIdentityOutboxMessagesAsync();
            foreach (var row in rows)
            {
                if (
                    TryGetAnyColumn(row, processedColumns, out var processedRaw)
                    && processedRaw is not null
                )
                {
                    return row;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        return null;
    }
}
