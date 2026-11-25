using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave3CheckInEndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave3CheckInStrongPassword#2026!";
    private static readonly string[] _goldTiers = ["Gold"];

    [Fact]
    public async Task CheckInEndpoints_AuthProtection_RoomBalancing_OpenApi_AndBlazorPage_WorkEndToEnd()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bootstrap:AutoMigrateAndSeed"] = "true",
                ["OpenApi:Enabled"] = "true",
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var scheduleId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var token = await OnboardAndLoginAsync(client);
        var configId = await CreateConfigurationAndAreaAsync(client, token, scheduleId, roomId);
        await VerifyEvaluateEndpointAndOpenApiAsync(client, token, configId, scheduleId, roomId);
        await VerifyCheckInBlazorPageAsync(client);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Check-In Test Organization",
                timeZoneId = "America/Denver",
                primaryBranchName = "Main Branch",
                primaryBranchShortCode = "MAIN",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var unauth = await client.GetAsync(
            new Uri("/api/checkin/configurations", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<Guid> CreateConfigurationAndAreaAsync(
        HttpClient client,
        string token,
        Guid scheduleId,
        Guid roomId
    )
    {
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/checkin/configurations")
        {
            Content = JsonContent.Create(
                new
                {
                    name = "Facility & Cohort Check-In",
                    profileKind = "ClassesAndPrograms",
                    searchMode = "PhoneAndName",
                }
            ),
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var createResp = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        Assert.NotNull(createResp.Headers.Location);

        using var doc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var configId = doc.RootElement.GetProperty("id").GetGuid();

        using var areaReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/checkin/configurations/{configId:D}/areas"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    configurationId = configId,
                    name = "Workshop Studio",
                    balancingStrategy = "LowestCapacityRatio",
                    eligibility = new { minAgeYears = 12, allowedMembershipTiers = _goldTiers },
                    rooms = new[]
                    {
                        new
                        {
                            locationId = roomId,
                            locationName = "Studio 201",
                            scheduleIds = new[] { scheduleId },
                            softCapacity = 15,
                            firmCapacity = 20,
                        },
                    },
                }
            ),
        };
        areaReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var areaResp = await client.SendAsync(areaReq);
        Assert.Equal(HttpStatusCode.OK, areaResp.StatusCode);

        return configId;
    }

    private static async Task VerifyEvaluateEndpointAndOpenApiAsync(
        HttpClient client,
        string token,
        Guid configId,
        Guid scheduleId,
        Guid roomId
    )
    {
        using var evalReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/checkin/configurations/{configId:D}/evaluate"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    configurationId = configId,
                    scheduleId,
                    participant = new { ageYears = 16, membershipTier = "Gold" },
                }
            ),
        };
        evalReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var evalResp = await client.SendAsync(evalReq);
        Assert.Equal(HttpStatusCode.OK, evalResp.StatusCode);

        using var evalDoc = JsonDocument.Parse(await evalResp.Content.ReadAsStringAsync());
        var areaOption = evalDoc.RootElement.GetProperty("areaOptions")[0];
        Assert.True(areaOption.GetProperty("isEligible").GetBoolean());
        Assert.Equal(roomId, areaOption.GetProperty("recommendedLocationId").GetGuid());

        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/checkin/configurations", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/checkin/configurations/{id}/evaluate",
            openApiText,
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyCheckInBlazorPageAsync(HttpClient client)
    {
        using var response = await client.GetAsync(
            new Uri("/admin/checkin/configurations", UriKind.Relative)
        );
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("FluentTextField", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fluent-text-field", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "An unhandled error has occurred",
            html,
            StringComparison.OrdinalIgnoreCase
        );
    }
}
