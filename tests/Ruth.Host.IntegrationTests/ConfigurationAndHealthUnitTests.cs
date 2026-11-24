using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.Health;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class ConfigurationAndHealthUnitTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-uri")]
    public async Task RustFsHealthCheck_WhenEndpointUnconfiguredOrInvalid_ReturnsUnhealthy(
        string? endpoint
    )
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = endpoint,
                }
            )
            .Build();
        var check = new RustFsHealthCheck(
            new UnusedHttpClientFactory(),
            config,
            Options.Create(new RustFsOptions()),
            Options.Create(new HealthChecksOptions()),
            NullLogger<RustFsHealthCheck>.Instance
        );

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("RustFS endpoint is not configured.", result.Description);
    }

    [Fact]
    public async Task RedisConnectionAccessor_WhenConnectTimesOut_CancelsAndDisposesCleanly()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:Redis"] = string.Create(
                        CultureInfo.InvariantCulture,
                        $"127.0.0.1:{port},abortConnect=false"
                    ),
                }
            )
            .Build();

        await using var accessor = new RedisConnectionAccessor(config);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            accessor.PingAsync(timeoutSeconds: 1, cts.Token)
        );
        listener.Stop();
    }

    private sealed class UnusedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            throw new InvalidOperationException("HttpClient should not be created.");
        }
    }
}
