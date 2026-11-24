using System.Net;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class HealthChecksConfigurationIntegrationTests(HostContainersFixture containers)
{
    [Fact]
    public async Task HealthReadiness_SupportsStorageEndpointFallbackOverride()
    {
        var fallbackOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = containers.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = containers.RedisConnectionString,
            ["Storage:Endpoint"] = containers.RustFsEndpoint,
            ["HealthChecks:TimeoutSeconds"] = "2",
        };
        await using var fallbackFactory = new AppConfigurationOnlyHostFactory(
            "Development",
            fallbackOverrides
        );
        using var client = fallbackFactory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReadiness_WhenRustFsEndpointIsInvalidUri_Returns503NotConfigured()
    {
        var fallbackOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = containers.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = containers.RedisConnectionString,
            ["Storage:RustFs:Endpoint"] = "not-a-valid-uri",
            ["HealthChecks:TimeoutSeconds"] = "2",
        };
        await using var factory = new AppConfigurationOnlyHostFactory(
            "Development",
            fallbackOverrides
        );
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var doc = JsonDocument.Parse(json);
        AssertCheckEntry(doc, "rustfs", "Unhealthy", "RustFS endpoint is not configured.");
    }

    [Fact]
    public async Task HealthReadiness_WhenRustFsEndpointEmptyAndStorageEndpointFallbackSet_Returns200Healthy()
    {
        var fallbackOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = containers.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = containers.RedisConnectionString,
            ["Storage:RustFs:Endpoint"] = string.Empty,
            ["Storage:Endpoint"] = containers.RustFsEndpoint,
            ["HealthChecks:TimeoutSeconds"] = "2",
        };
        await using var factory = new AppConfigurationOnlyHostFactory(
            "Development",
            fallbackOverrides
        );
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        AssertCheckEntry(doc, "rustfs", "Healthy", "RustFS is reachable.");
    }

    [Fact]
    public async Task HealthReadiness_WhenRustFsEndpointIsEmptyWithoutFallback_Returns503NotConfigured()
    {
        var emptyOverride = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = containers.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = containers.RedisConnectionString,
            ["Storage:RustFs:Endpoint"] = string.Empty,
            ["HealthChecks:TimeoutSeconds"] = "2",
        };
        await using var factory = new AppConfigurationOnlyHostFactory("Development", emptyOverride);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("DataAnnotation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "OptionsValidationException",
            json,
            StringComparison.OrdinalIgnoreCase
        );
        using var doc = JsonDocument.Parse(json);
        AssertCheckEntry(doc, "rustfs", "Unhealthy", "RustFS endpoint is not configured.");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("999")]
    public async Task HealthReadiness_WhenTimeoutSecondsOutOfRange_Returns503WithoutValidationLeak(
        string timeoutSeconds
    )
    {
        var invalidTimeout = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = containers.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = containers.RedisConnectionString,
            ["Storage:RustFs:Endpoint"] = containers.RustFsEndpoint,
            ["HealthChecks:TimeoutSeconds"] = timeoutSeconds,
        };
        await using var factory = new AppConfigurationOnlyHostFactory(
            "Development",
            invalidTimeout
        );
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("DataAnnotation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "OptionsValidationException",
            json,
            StringComparison.OrdinalIgnoreCase
        );

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Unhealthy", doc.RootElement.GetProperty("status").GetString());
        AssertCheckEntry(doc, "postgresql", "Unhealthy", "PostgreSQL readiness check failed.");
        AssertCheckEntry(doc, "redis", "Unhealthy", "Redis readiness check failed.");
        AssertCheckEntry(doc, "rustfs", "Unhealthy", "RustFS readiness check failed.");
    }

    private static void AssertCheckEntry(
        JsonDocument doc,
        string checkName,
        string expectedStatus,
        string expectedDescription
    )
    {
        var entry = doc
            .RootElement.GetProperty("checks")
            .EnumerateArray()
            .FirstOrDefault(e =>
                string.Equals(
                    e.GetProperty("name").GetString(),
                    checkName,
                    StringComparison.Ordinal
                )
            );
        Assert.Equal(expectedStatus, entry.GetProperty("status").GetString());
        Assert.Equal(expectedDescription, entry.GetProperty("description").GetString());
    }
}
