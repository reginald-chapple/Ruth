using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class HealthChecksIntegrationTests(HostContainersFixture containers)
{
    [Fact]
    public async Task HealthLive_WhenAllDependenciesOffline_Returns200Healthy()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies();
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task HealthReadiness_WhenAllContainersHealthy_Returns200WithAllChecksHealthy()
    {
        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            containers.RedisConnectionString,
            containers.RustFsEndpoint
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());

        var checks = doc.RootElement.GetProperty("checks").EnumerateArray().ToArray();
        Assert.Equal(3, checks.Length);
        Assert.All(checks, static check =>
            Assert.Equal("Healthy", check.GetProperty("status").GetString())
        );
    }

    [Fact]
    public async Task HealthReadiness_WhenPostgresUnreachable_Returns503WithinBoundedTimeout()
    {
        var deadPort = HostWebApplicationFactory.GetUnusedLoopbackPort();
        await using var factory = HostWebApplicationFactory.Create(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Host=127.0.0.1;Port={deadPort};Database=ruth;Username=ruth;Password=ruth_secret;Timeout=1"
            ),
            containers.RedisConnectionString,
            containers.RustFsEndpoint
        );

        await AssertUnhealthyCheckAsync(factory, "postgresql");
    }

    [Fact]
    public async Task HealthReadiness_WhenRedisUnreachable_Returns503WithinBoundedTimeout()
    {
        var deadPort = HostWebApplicationFactory.GetUnusedLoopbackPort();
        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            string.Create(
                CultureInfo.InvariantCulture,
                $"127.0.0.1:{deadPort},abortConnect=false,connectTimeout=500"
            ),
            containers.RustFsEndpoint
        );

        await AssertUnhealthyCheckAsync(factory, "redis");
    }

    [Fact]
    public async Task HealthReadiness_WhenRustFsUnreachable_Returns503WithinBoundedTimeout()
    {
        var deadPort = HostWebApplicationFactory.GetUnusedLoopbackPort();
        await using var factory = HostWebApplicationFactory.Create(
            containers.PostgresConnectionString,
            containers.RedisConnectionString,
            string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{deadPort}")
        );

        await AssertUnhealthyCheckAsync(factory, "rustfs");
    }

    [Fact]
    public async Task HealthEndpoints_PostMethod_Returns405MethodNotAllowed()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies();
        using var client = factory.CreateNoRedirectClient();
        using var empty = new StringContent(string.Empty);

        using var liveResp = await client.PostAsync(
            new Uri("/health/live", UriKind.Relative),
            empty
        );
        using var readyResp = await client.PostAsync(new Uri("/health", UriKind.Relative), empty);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, liveResp.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, readyResp.StatusCode);
    }

    private static async Task AssertUnhealthyCheckAsync(
        HostWebApplicationFactory factory,
        string failingCheckName
    )
    {
        using var client = factory.CreateNoRedirectClient();
        var start = Stopwatch.GetTimestamp();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var elapsed = Stopwatch.GetElapsedTime(start);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(elapsed < TimeSpan.FromSeconds(10), $"Health check took {elapsed}.");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Unhealthy", doc.RootElement.GetProperty("status").GetString());

        var failedEntry = doc
            .RootElement.GetProperty("checks")
            .EnumerateArray()
            .FirstOrDefault(e =>
                string.Equals(
                    e.GetProperty("name").GetString(),
                    failingCheckName,
                    StringComparison.Ordinal
                )
            );
        Assert.Equal("Unhealthy", failedEntry.GetProperty("status").GetString());
        Assert.DoesNotContain("Password=", json, StringComparison.OrdinalIgnoreCase);
    }
}
