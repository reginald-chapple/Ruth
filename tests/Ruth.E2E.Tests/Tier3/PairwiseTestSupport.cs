using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier3;

internal static partial class PairwiseTestSupport
{
    [GeneratedRegex(
        "(?:href|src)=[\"'](?<path>[^\"']*(?:_content|_framework|default-fuib[^\"']*\\.css|blazor\\.web[^\"']*\\.js)[^\"']*)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 2000
    )]
    private static partial Regex AssetHrefOrSrcRegex { get; }

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

    public static void AssertProblemDetailsResponse(HttpResponseMessage response, string body)
    {
        Assert.False(response.IsSuccessStatusCode);
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("json", mediaType, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var hasStatusOrTitle =
            root.TryGetProperty("status", out _) || root.TryGetProperty("title", out _);
        Assert.True(hasStatusOrTitle, $"Expected RFC 7807 ProblemDetails JSON, got: {body}");
    }

    public static void AssertNoLeakageInErrorBody(string body, string? secret = null)
    {
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("System.Text.Json.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql.", body, StringComparison.Ordinal);

        if (!string.IsNullOrEmpty(secret))
        {
            Assert.DoesNotContain(secret, body, StringComparison.Ordinal);
        }
    }

    public static IReadOnlyList<string> ExtractStaticAssetPaths(string html)
    {
        var matches = AssetHrefOrSrcRegex.Matches(html);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in matches)
        {
            var raw = match.Groups["path"].Value;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var normalized = raw.StartsWith('/') ? raw : "/" + raw;
                _ = paths.Add(normalized);
            }
        }

        return [.. paths];
    }

    public static int GetUnusedTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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
        Assert.True(
            DatabaseProbe.TryGetColumnValue(row, columnName, out var rawValue),
            $"Expected column '{columnName}' to exist."
        );
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

    public static void AssertOutboxRowValid(
        IReadOnlyDictionary<string, object?> row,
        string expectedOrganizationName
    )
    {
        Assert.True(DatabaseProbe.TryGetColumnValue(row, "Id", out var idObj));
        var outboxId = Assert.IsType<Guid>(idObj);
        TestOnboardingPayloads.AssertIsUuidV7(outboxId);

        Assert.True(DatabaseProbe.TryGetColumnValue(row, "EventType", out var eventTypeObj));
        var eventType = Assert.IsType<string>(eventTypeObj);
        Assert.Contains("OrganizationOnboarded", eventType, StringComparison.Ordinal);

        Assert.True(DatabaseProbe.TryGetColumnValue(row, "Payload", out var payloadObj));
        var payloadJson = Assert.IsType<string>(payloadObj);
        using var payloadDoc = JsonDocument.Parse(payloadJson);
        Assert.Contains(expectedOrganizationName, payloadJson, StringComparison.Ordinal);

        var occurredOnUtc = ReadUtcDateTimeOffset(row, "OccurredOnUtc");
        TestOnboardingPayloads.AssertUtcTimestamp(occurredOnUtc);
    }
}
