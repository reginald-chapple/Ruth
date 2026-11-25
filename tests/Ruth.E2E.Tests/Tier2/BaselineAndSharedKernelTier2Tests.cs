using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class BaselineAndSharedKernelTier2Tests(E2EContainersFixture containers)
{
    private static readonly string[] _contractDtoNames =
    [
        "OnboardingStatusResponse",
        "GetOnboardingStatusQuery",
        "CompleteOnboardingCommand",
        "CompleteOnboardingResponse",
        "OrganizationOnboardedIntegrationEvent",
    ];

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public void Baseline_DirectoryBuildProps_EnforcesStrictAnalyzerAndNet10Settings()
    {
        var root = Tier2TestHelpers.FindRepositoryRoot();
        var props = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));

        Assert.Contains(
            "<TargetFramework>net10.0</TargetFramework>",
            props,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "<TreatWarningsAsErrors>true</TreatWarningsAsErrors>",
            props,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "<EnableNETAnalyzers>true</EnableNETAnalyzers>",
            props,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("<NoWarn>", props, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Baseline_EditorConfigAndCodeMetrics_MaintainStrictBounds()
    {
        var root = Tier2TestHelpers.FindRepositoryRoot();
        var editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        var metrics = File.ReadAllText(Path.Combine(root, "CodeMetricsConfig.txt"));

        Assert.Contains(
            "dotnet_analyzer_diagnostic.category-Security.severity = error",
            editorConfig,
            StringComparison.Ordinal
        );
        Assert.Contains("max_line_length = 100", editorConfig, StringComparison.Ordinal);
        Assert.Contains("CA1502: 15", metrics, StringComparison.Ordinal);
        Assert.Contains("CA1501: 4", metrics, StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_HostAndContractsProjects_ContainNoSwashbuckleOrNoWarn()
    {
        var root = Tier2TestHelpers.FindRepositoryRoot();
        var hostPath = Path.Combine(root, "src", "Ruth.Host", "Ruth.Host.csproj");
        var contractsPath = Path.Combine(
            root,
            "src",
            "Modules",
            "Identity",
            "Ruth.Modules.Identity.Contracts",
            "Ruth.Modules.Identity.Contracts.csproj"
        );
        var hostCsproj = File.ReadAllText(hostPath);
        var contractsCsproj = File.ReadAllText(contractsPath);

        Assert.DoesNotContain("Swashbuckle", hostCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<NoWarn>", hostCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<NoWarn>", contractsCsproj, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Baseline_UnknownApiRoute_InProduction_Returns404WithoutStackTrace()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn, "Production");
            using var client = factory.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(client);

            using var response = await client.GetAsync(
                new Uri("/api/nonexistent-boundary-route", UriKind.Relative)
            );
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Tier2TestHelpers.AssertNoStackTraceLeak(body);
        });
    }

    [Fact]
    public async Task Baseline_UnknownStaticPath_ReturnsNotFoundWithoutDirectoryListing()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            await Tier2TestHelpers.CompleteOnboardingAsync(client);

            using var response = await client.GetAsync(
                new Uri("/_content/MissingPkg/missing.js", UriKind.Relative)
            );
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("Index of /", body, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task SharedKernel_MalformedJsonPayload_ReturnsBadRequestProblemDetails()
    {
        await AssertRejectedOnboardingJsonAsync("{not-valid-json");
    }

    [Fact]
    public async Task SharedKernel_EmptyJsonObject_ReturnsBadRequestProblemDetails()
    {
        await AssertRejectedOnboardingJsonAsync("{}");
    }

    [Fact]
    public async Task SharedKernel_NullRequiredFields_ReturnsBadRequestProblemDetails()
    {
        const string nullFieldsJson =
            "{\"organizationName\":null,\"timeZoneId\":null,\"primaryBranchName\":null,"
            + "\"adminUserName\":null,\"adminEmail\":null,\"adminDisplayName\":null,"
            + "\"adminPassword\":null}";
        await AssertRejectedOnboardingJsonAsync(nullFieldsJson);
    }

    [Fact]
    public async Task SharedKernel_WrongJsonFieldType_ReturnsBadRequestWithoutServerError()
    {
        var payload = TestOnboardingPayloads.CreateValidPayload();
        payload["organizationName"] = JsonValue.Create(12345);
        await AssertRejectedOnboardingJsonAsync(payload.ToJsonString());
    }

    [Fact]
    public async Task SharedKernel_OversizedOrganizationName_ReturnsBadRequestWithZeroRows()
    {
        var payload = TestOnboardingPayloads.CreateOversizedOrganizationPayload();
        await AssertRejectedOnboardingJsonAsync(payload.ToJsonString());
    }

    [Fact]
    public void SharedKernel_IdentityContractsDtos_AreSealedAndUseDateTimeOffset()
    {
        var assembly = Assembly.Load("Ruth.Modules.Identity.Contracts");
        var exportedTypes = assembly.GetExportedTypes();

        foreach (var dtoName in _contractDtoNames)
        {
            var type = exportedTypes.SingleOrDefault(t =>
                string.Equals(t.Name, dtoName, StringComparison.Ordinal)
            );
            Assert.NotNull(type);
            Assert.True(type.IsSealed, $"Expected '{type.FullName}' to be sealed.");
            Assert.All(type.GetProperties(), p =>
                Assert.NotEqual(typeof(DateTime), p.PropertyType)
            );
        }
    }

    private async Task AssertRejectedOnboardingJsonAsync(string rawJson)
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var response = await Tier2TestHelpers.PostOnboardingJsonAsync(client, rawJson);

            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest
            );
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);
        });
    }
}
