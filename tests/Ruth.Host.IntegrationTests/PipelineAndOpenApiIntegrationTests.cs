using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed partial class PipelineAndOpenApiIntegrationTests
{
    [Fact]
    public async Task OpenApi_InDevelopment_ReturnsValidOpenApi3JsonWithoutSwashbuckle()
    {
        var referencedAssemblies = typeof(Program)
            .Assembly.GetReferencedAssemblies()
            .Select(static a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referencedAssemblies, static name =>
            name.Contains("Swashbuckle", StringComparison.OrdinalIgnoreCase)
        );

        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Development"
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Swashbuckle", json, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("openapi").GetString();
        Assert.NotNull(version);
        Assert.StartsWith("3.", version, StringComparison.Ordinal);

        var paths = doc.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health/live", out _));
        Assert.True(paths.TryGetProperty("/health", out _));

        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("HealthStatus", out var healthStatusSchema));
        var enumValues = healthStatusSchema
            .GetProperty("enum")
            .EnumerateArray()
            .Select(static e => e.GetString())
            .ToArray();
        Assert.Contains("Healthy", enumValues, StringComparer.Ordinal);
        Assert.Contains("Unhealthy", enumValues, StringComparer.Ordinal);
    }

    [Fact]
    public async Task OpenApi_InProduction_Returns404NotFound()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Production"
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Pipeline_UnknownApiRoute_Returns404WithoutHtmlOrStackTrace()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Production"
        );
        using var client = factory.CreateNoRedirectClient();

        using var response = await client.GetAsync(
            new Uri("/api/nonexistent-endpoint-check", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaticAssets_AreServedWith200OkViaMapStaticAssets()
    {
        await using var factory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Development"
        );
        using var client = factory.CreateNoRedirectClient();

        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative));
        var assetPaths = AssetPathRegex
            .Matches(html)
            .Select(static match => match.Groups["url"].Value)
            .Where(static url => url.StartsWith('/'))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(assetPaths);
        Assert.Contains(assetPaths, static p =>
            p.Contains("default-fuib", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(assetPaths, static p =>
            p.Contains("Ruth.Host", StringComparison.OrdinalIgnoreCase)
            && p.Contains("styles", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(assetPaths, static p =>
            p.Contains("app", StringComparison.OrdinalIgnoreCase)
        );
        Assert.Contains(assetPaths, static p =>
            p.Contains("blazor.web", StringComparison.OrdinalIgnoreCase)
        );

        foreach (var path in assetPaths)
        {
            using var assetResp = await client.GetAsync(new Uri(path, UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, assetResp.StatusCode);
        }
    }

    [GeneratedRegex(
        """(?:href|src)=["'](?<url>/(?:_content|_framework|Ruth\.Host\.[^"']*styles|app)[^"']+)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex AssetPathRegex { get; }
}
