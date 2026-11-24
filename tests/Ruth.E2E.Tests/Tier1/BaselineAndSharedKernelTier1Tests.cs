using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier1;

[Collection(E2ETestCollection.Name)]
public sealed class BaselineAndSharedKernelTier1Tests(E2EContainersFixture containers)
{
    private static readonly string[] _contractDtoNames =
    [
        "OnboardingStatusResponse",
        "CompleteOnboardingCommand",
        "CompleteOnboardingResponse",
        "OrganizationOnboardedIntegrationEvent",
    ];

    [Fact]
    public void Feature01_HostAndContractsAssemblies_TargetNet10_AndExposePublicContracts()
    {
        var hostAssembly = typeof(Program).Assembly;
        var contractsAssembly = Assembly.Load(new AssemblyName("Ruth.Modules.Identity.Contracts"));

        var hostTarget = hostAssembly.GetCustomAttribute<TargetFrameworkAttribute>();
        var contractsTarget = contractsAssembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.NotNull(hostTarget);
        Assert.NotNull(contractsTarget);
        Assert.Contains(
            ".NETCoreApp,Version=v10.0",
            hostTarget.FrameworkName,
            StringComparison.Ordinal
        );
        Assert.Contains(
            ".NETCoreApp,Version=v10.0",
            contractsTarget.FrameworkName,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Feature01_IdentityContractDtos_ArePublicSealedTypes()
    {
        var contractsAssembly = Assembly.Load(new AssemblyName("Ruth.Modules.Identity.Contracts"));
        var exportedTypes = contractsAssembly.GetExportedTypes();

        foreach (var dtoName in _contractDtoNames)
        {
            var match = exportedTypes.FirstOrDefault(t =>
                string.Equals(t.Name, dtoName, StringComparison.Ordinal)
            );
            Assert.NotNull(match);
            Assert.True(match.IsPublic, $"Expected contract '{dtoName}' to be public.");
            Assert.True(match.IsSealed, $"Expected contract '{dtoName}' to be sealed.");
        }
    }

    [Fact]
    public async Task Feature01_HostBoot_SucceedsInDevelopment_AndServesLiveness()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feature01_HostReadinessProbe_SucceedsAgainstLiveContainers()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feature01_HostExposesOpenApiAndOnboardingEndpointsOnBoot()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();

        using var openApiResponse = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        using var onboardingResponse = await client.GetAsync(
            new Uri("/onboarding", UriKind.Relative)
        );

        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, onboardingResponse.StatusCode);
    }

    [Fact]
    public async Task Feature02_OnboardingResponse_ReturnsVersion7OrganizationAndAdminIds()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var doc = await Tier1TestSupport.CompleteOnboardingAsync(client);

        var orgId = doc.RootElement.GetProperty("organizationId").GetGuid();
        var adminUserId = doc.RootElement.GetProperty("adminUserId").GetGuid();

        TestOnboardingPayloads.AssertIsUuidV7(orgId);
        TestOnboardingPayloads.AssertIsUuidV7(adminUserId);
    }

    [Fact]
    public async Task Feature02_SuccessiveOnboardingRuns_ProduceMonotonicallyOrderedUuidV7Ids()
    {
        await using var firstScope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var firstClient = firstScope.Factory.CreateClient();
        using var firstDoc = await Tier1TestSupport.CompleteOnboardingAsync(firstClient);

        await Task.Delay(TimeSpan.FromMilliseconds(15));

        await using var secondScope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var secondClient = secondScope.Factory.CreateClient();
        using var secondDoc = await Tier1TestSupport.CompleteOnboardingAsync(secondClient);

        var firstOrgId = firstDoc.RootElement.GetProperty("organizationId").GetGuid();
        var secondOrgId = secondDoc.RootElement.GetProperty("organizationId").GetGuid();
        var firstAdminId = firstDoc.RootElement.GetProperty("adminUserId").GetGuid();
        var secondAdminId = secondDoc.RootElement.GetProperty("adminUserId").GetGuid();

        TestOnboardingPayloads.AssertUuidV7Ordered(firstOrgId, secondOrgId);
        TestOnboardingPayloads.AssertUuidV7Ordered(firstAdminId, secondAdminId);
    }

    [Fact]
    public async Task Feature02_OnboardingResponse_ReturnsIso8601UtcCompletedTimestamp()
    {
        var beforeUtc = DateTimeOffset.UtcNow.AddSeconds(-2);
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var doc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        var afterUtc = DateTimeOffset.UtcNow.AddSeconds(2);

        var rawTimestamp = doc.RootElement.GetProperty("completedOnUtc").GetString();
        Assert.False(string.IsNullOrWhiteSpace(rawTimestamp));

        var parsed = DateTimeOffset.Parse(
            rawTimestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind
        );
        TestOnboardingPayloads.AssertUtcTimestampInRange(parsed, beforeUtc, afterUtc);
    }

    [Fact]
    public async Task Feature02_ValidationFailure_MapsToRfc7807ProblemDetails()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        var invalidPayload = TestOnboardingPayloads.CreateMissingOrganizationNamePayload();

        using var content = TestOnboardingPayloads.ToJsonContent(invalidPayload);
        using var response = await client.PostAsync(
            new Uri(Tier1TestSupport.OnboardingEndpoint, UriKind.Relative),
            content
        );

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected 400/422 ProblemDetails, got {0}.",
                (int)response.StatusCode
            )
        );
        AssertProblemDetailsResponse(response, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Feature02_ConflictFailure_MapsToRfc7807ProblemDetails()
    {
        await using var scope = await Tier1TestSupport.CreateIsolatedHostAsync(containers);
        using var client = scope.Factory.CreateClient();
        using var initialDoc = await Tier1TestSupport.CompleteOnboardingAsync(client);
        Assert.NotNull(initialDoc);

        using var content = TestOnboardingPayloads.ToJsonContent(
            TestOnboardingPayloads.CreateValidPayload()
        );
        using var replayResponse = await client.PostAsync(
            new Uri(Tier1TestSupport.OnboardingEndpoint, UriKind.Relative),
            content
        );

        Assert.True(
            replayResponse.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden,
            string.Format(
                CultureInfo.InvariantCulture,
                "Expected 409/403 ProblemDetails on replay, got {0}.",
                (int)replayResponse.StatusCode
            )
        );
        AssertProblemDetailsResponse(
            replayResponse,
            await replayResponse.Content.ReadAsStringAsync()
        );
    }

    private static void AssertProblemDetailsResponse(HttpResponseMessage response, string body)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.Contains("problem+json", mediaType, StringComparison.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("status", out var statusElement));
        Assert.Equal((int)response.StatusCode, statusElement.GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("title", out var titleElement));
        Assert.False(string.IsNullOrWhiteSpace(titleElement.GetString()));
    }
}
