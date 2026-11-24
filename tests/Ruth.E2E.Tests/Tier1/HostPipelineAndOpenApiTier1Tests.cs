using System.Net;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class HostPipelineAndOpenApiTier1Tests(E2EContainersFixture containers)
{
    [Fact]
    public async Task Feature08_Pipeline_ServesHealthLiveWithJsonContentType()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Feature08_Pipeline_SerializesStatusStringsViaConfiguredJsonOptions()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString();
        Assert.Equal("Healthy", status);
    }

    [Fact]
    public async Task Feature08_Pipeline_MapsInvalidInputToRfc7807ProblemDetails()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var content = TestOnboardingPayloads.ToJsonContent(
            TestOnboardingPayloads.CreateInvalidEmailPayload()
        );
        using var response = await client.PostAsync(
            new Uri(Tier1TestSupport.OnboardingEndpoint, UriKind.Relative),
            content
        );

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity
        );
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("problem+json", mediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature08_Pipeline_Returns404WithoutStackTraceForUnknownApiRoute()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateNoRedirectClient();
        using var onboardingDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(onboardingDoc);

        using var response = await client.GetAsync(
            new Uri("/api/non-existent-tier1-route", UriKind.Relative)
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("System.Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Feature08_Pipeline_ServesStaticAssetsFromMapStaticAssets()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        var onboardingHtml = await client.GetStringAsync(new Uri("/onboarding", UriKind.Relative));
        var assetPath = Tier1TestSupport.ExtractFirstAssetPath(onboardingHtml, "blazor.web")
            ?? Tier1TestSupport.ExtractFirstAssetPath(onboardingHtml, "_content/");
        Assert.NotNull(assetPath);

        using var assetResponse = await client.GetAsync(new Uri(assetPath, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, assetResponse.StatusCode);
    }

    [Fact]
    public async Task Feature09_OpenApiEndpoint_Returns200JsonInDevelopment()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Feature09_OpenApiDocument_HasValidOpenApi3VersionAndInfo()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        using var doc = JsonDocument.Parse(json);
        var openApiVersion = doc.RootElement.GetProperty("openapi").GetString();
        var info = doc.RootElement.GetProperty("info");
        var title = info.GetProperty("title").GetString();
        var version = info.GetProperty("version").GetString();

        Assert.NotNull(openApiVersion);
        Assert.StartsWith("3.", openApiVersion, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    [Fact]
    public async Task Feature09_OpenApiDocument_DocumentsHealthAndOnboardingPaths()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        using var doc = JsonDocument.Parse(json);
        var paths = doc.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/health/live", out var healthPath));
        Assert.True(healthPath.TryGetProperty("get", out _));
        Assert.True(paths.TryGetProperty("/api/onboarding", out var onboardingPath));
        Assert.True(onboardingPath.TryGetProperty("post", out _));
    }

    [Fact]
    public async Task Feature09_OpenApiDocument_IncludesDtoSchemas()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        using var doc = JsonDocument.Parse(json);
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        var schemaNames = new List<string>();
        foreach (var prop in schemas.EnumerateObject())
        {
            schemaNames.Add(prop.Name);
        }

        Assert.NotEmpty(schemaNames);
        Assert.Contains(schemaNames, name =>
            name.Contains("Onboarding", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public async Task Feature09_HostAssemblyAndOpenApiDoc_ContainZeroSwashbuckleMarkers()
    {
        var referencedNames = typeof(Program)
            .Assembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty);

        Assert.DoesNotContain(referencedNames, name =>
            name.Contains("Swashbuckle", StringComparison.OrdinalIgnoreCase)
        );

        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var json = await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.DoesNotContain("Swashbuckle", json, StringComparison.OrdinalIgnoreCase);
    }
}
