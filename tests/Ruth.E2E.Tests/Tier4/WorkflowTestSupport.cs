using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier4;

internal static class WorkflowTestSupport
{
    public static async Task RunWithIsolatedDatabaseAsync(
        E2EContainersFixture containers,
        Func<string, Task> action
    )
    {
        var databaseName = await containers.CreateIsolatedDatabaseAsync();
        try
        {
            await action(databaseName);
        }
        finally
        {
            await containers.DropDatabaseAsync(databaseName);
        }
    }

    public static Dictionary<string, string?> BuildHostEnvironment(
        E2EContainersFixture containers,
        string databaseName,
        string environment,
        string signingKey
    )
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["Authentication__Jwt__SigningKey"] = signingKey,
            ["ConnectionStrings__RuthDatabase"] = containers.GetConnectionString(databaseName),
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
            ["Bootstrap__AutoMigrateAndSeed"] = "true",
        };
    }

    public static async Task<JsonDocument> CompleteOnboardingAsync(
        HttpClient client,
        JsonObject? payload = null
    )
    {
        using var content = TestOnboardingPayloads.ToJsonContent(
            payload ?? TestOnboardingPayloads.CreateValidPayload()
        );
        using var response = await client.PostAsync("/api/onboarding", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected 200/201 from POST /api/onboarding, got {0}: {1}",
                response.StatusCode,
                body
            )
        );

        return JsonDocument.Parse(body);
    }

    public static async Task AssertRedirectAsync(
        HttpClient noRedirectClient,
        string requestPath,
        string expectedLocationPrefix
    )
    {
        using var response = await noRedirectClient.GetAsync(requestPath);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith(expectedLocationPrefix, location, StringComparison.OrdinalIgnoreCase);
    }

    public static void AssertProblemDetailsAndNoLeaks(
        HttpResponseMessage response,
        string body,
        string? secret = null
    )
    {
        Assert.False(response.IsSuccessStatusCode);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("json", mediaType, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("status", out _) || root.TryGetProperty("title", out _));

        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(secret))
        {
            Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
        }
    }

    public static async Task<IReadOnlyDictionary<string, object?>> WaitForProcessedOutboxAsync(
        DatabaseProbe probe,
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
                    DatabaseProbe.TryGetColumnValue(row, "ProcessedOnUtc", out var processed)
                    && processed is DateTimeOffset or DateTime
                )
                {
                    return row;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(400));
        }

        throw new TimeoutException(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Outbox message did not reach ProcessedOnUtc within {timeout.TotalSeconds}s."
            )
        );
    }

    public static DateTimeOffset ReadUtcDateTimeOffset(
        IReadOnlyDictionary<string, object?> row,
        string columnName
    )
    {
        Assert.True(DatabaseProbe.TryGetColumnValue(row, columnName, out var rawValue));
        Assert.NotNull(rawValue);

        return rawValue switch
        {
            DateTimeOffset dto => dto.ToUniversalTime(),
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException(
                $"Column '{columnName}' had unexpected type '{rawValue.GetType().FullName}'."
            ),
        };
    }

    public static async Task WaitForHealthStatusAsync(
        HttpClient client,
        HttpStatusCode expectedStatus,
        TimeSpan timeout
    )
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        HttpStatusCode? lastStatus = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync("/health");
            lastStatus = response.StatusCode;
            if (lastStatus == expectedStatus)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(350));
        }

        throw new TimeoutException(
            string.Format(
                CultureInfo.InvariantCulture,
                "GET /health did not reach {0} within {1}s (last: {2}).",
                expectedStatus,
                timeout.TotalSeconds,
                lastStatus
            )
        );
    }
}
