using System.Net;
using Microsoft.Extensions.Options;
using Ruth.Host.Configuration;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class ProductionSecurityGuardIntegrationTests
{
    [Fact]
    public async Task Production_WithoutSigningKey_FailsFastOnStartup()
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        await using var factory = new AppConfigurationOnlyHostFactory("Production", settings);

        var exception = Assert.Throws<OptionsValidationException>(factory.CreateClient);
        Assert.Contains("SigningKey", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(
            JwtOptions.DevelopmentDefaultSigningKey,
            exception.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890123456789012345678901")] // 31 chars
    [InlineData(JwtOptions.DevelopmentDefaultSigningKey)]
    public async Task Production_WithWeakOrPlaceholderSigningKey_FailsFastWithoutEchoingSecret(
        string? invalidKey
    )
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authentication:Jwt:SigningKey"] = invalidKey,
        };

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Production",
            overrides: overrides
        );

        var exception = Assert.Throws<OptionsValidationException>(factory.CreateNoRedirectClient);
        Assert.Contains("32 characters", exception.Message, StringComparison.Ordinal);

        if (!string.IsNullOrWhiteSpace(invalidKey))
        {
            Assert.DoesNotContain(invalidKey, exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Production_WithValid32CharSigningKey_BootsAndServesHealthLive()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authentication:Jwt:SigningKey"] = "prod-secret-key-with-at-least-32-random-chars!",
        };

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Production",
            overrides: overrides
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Development_WithPlaceholderSigningKey_BootsCleanly()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authentication:Jwt:SigningKey"] = JwtOptions.DevelopmentDefaultSigningKey,
        };

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            environment: "Development",
            overrides: overrides
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
