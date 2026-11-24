using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class RateLimitingIntegrationTests
{
    [Fact]
    public async Task RateLimiting_Returns429ProblemDetailsForApiWhileExemptingHealthEndpoints()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RateLimiting:PermitLimit"] = "2",
            ["RateLimiting:WindowSeconds"] = "60",
        };

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Development",
            overrides: overrides
        );
        using var client = factory.CreateNoRedirectClient();

        using var first = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var second = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var third = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains(
            "application/problem+json",
            third.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );

        for (var i = 0; i < 4; i++)
        {
            using var liveResp = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, liveResp.StatusCode);
        }
    }

    [Fact]
    public async Task RateLimiting_WithConfigureAppConfigurationOnly_DoesNotDoubleChargeNotFound()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RateLimiting:PermitLimit"] = "2",
            ["RateLimiting:WindowSeconds"] = "60",
            ["HealthChecks:TimeoutSeconds"] = "1",
        };

        await using var factory = new AppConfigurationOnlyHostFactory(
            "Development",
            settings,
            static services =>
                _ = services.Replace(
                    ServiceDescriptor.Singleton<IOnboardingStateReader>(
                        new CompletedOnboardingStateReaderStub()
                    )
                )
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        using var first = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var second = await client.GetAsync(new Uri("/does-not-exist", UriKind.Relative));
        using var third = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var healthLive = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        using var healthReady = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Contains(
            "application/problem+json",
            third.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.Equal(HttpStatusCode.OK, healthLive.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, healthReady.StatusCode);
    }

    [Fact]
    public async Task RateLimiting_InvalidPermitLimit_ThrowsOptionsValidationExceptionOnStartup()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RateLimiting:PermitLimit"] = "-5",
        };

        await using var factory = new AppConfigurationOnlyHostFactory(
            "Development",
            settings,
            static services =>
                _ = services.Replace(
                    ServiceDescriptor.Singleton<IOnboardingStateReader>(
                        new CompletedOnboardingStateReaderStub()
                    )
                )
        );
        _ = Assert.Throws<OptionsValidationException>(factory.CreateClient);
    }

    [Fact]
    public async Task RateLimiting_DynamicConfigurationReload_UpdatesActivePartitionImmediately()
    {
        var source = new ReloadableMemoryConfigurationSource(
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["RateLimiting:PermitLimit"] = "1",
                ["RateLimiting:WindowSeconds"] = "60",
                ["HealthChecks:TimeoutSeconds"] = "1",
            }
        );

        await using var factory = new ReloadableConfigurationHostFactory("Development", source);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        using var first = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var second = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);

        source.Provider.SetAndReload("RateLimiting:PermitLimit", "10");

        var monitor = factory.Services.GetRequiredService<IOptionsMonitor<RateLimitingOptions>>();
        Assert.Equal(10, monitor.CurrentValue.PermitLimit);

        using var afterRelax1 = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        using var afterRelax2 = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, afterRelax1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, afterRelax2.StatusCode);

        source.Provider.SetAndReload("RateLimiting:PermitLimit", "2");
        Assert.Equal(2, monitor.CurrentValue.PermitLimit);

        using var tightened1 = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var tightened2 = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        using var tightened3 = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, tightened1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tightened2.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, tightened3.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RateLimiting_AcceptTextHtml_Returns429ProblemDetailsWithoutRedirectOrNotFoundHtml(
        bool isOnboardingRequired
    )
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RateLimiting:PermitLimit"] = "1",
            ["RateLimiting:WindowSeconds"] = "60",
        };

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Development",
            overrides: overrides,
            assumeOnboarded: false,
            configureServices: services =>
                _ = services.Replace(
                    ServiceDescriptor.Singleton<IOnboardingStateReader>(
                        new CompletedOnboardingStateReaderStub(
                            isOnboardingRequired: isOnboardingRequired
                        )
                    )
                )
        );
        using var client = factory.CreateNoRedirectClient();

        using var first = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        using var second = await client.SendAsync(request);
        var body = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Null(second.Headers.Location);
        Assert.Contains(
            "application/problem+json",
            second.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );

        using var doc = JsonDocument.Parse(body);
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Too Many Requests", doc.RootElement.GetProperty("title").GetString());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }
}
