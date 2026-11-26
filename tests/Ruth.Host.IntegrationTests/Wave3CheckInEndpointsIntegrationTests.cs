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
        var (configId, areaId) = await CreateConfigurationAndAreaAsync(
            client,
            token,
            scheduleId,
            roomId
        );
        await VerifyEvaluateEndpointAndOpenApiAsync(client, token, configId, scheduleId, roomId);
        await VerifyMobileAndRoomRosterEndpointsAsync(client, token, configId, areaId, scheduleId);
        await VerifyWalletPassAndPassKitEndpointsAsync(client, token);
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

    private static async Task<(Guid ConfigId, Guid AreaId)> CreateConfigurationAndAreaAsync(
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
                    securityCodeAlphaLength = 2,
                    securityCodeNumericLength = 2,
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
                    issueSecurityPickupCode = true,
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

        using var areaDoc = JsonDocument.Parse(await areaResp.Content.ReadAsStringAsync());
        var areaId = areaDoc.RootElement.GetProperty("areas")[0].GetProperty("id").GetGuid();
        return (configId, areaId);
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

    private static async Task VerifyMobileAndRoomRosterEndpointsAsync(
        HttpClient client,
        string token,
        Guid configId,
        Guid areaId,
        Guid scheduleId
    )
    {
        using var mobileReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/checkin/mobile/sessions"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    configurationId = configId,
                    scheduleId,
                    items = new[]
                    {
                        new
                        {
                            personId = Guid.CreateVersion7(),
                            areaId,
                            personFullName = "Jordan Patel",
                            participantContext = new { ageYears = 18, membershipTier = "Gold" },
                        },
                    },
                }
            ),
        };
        mobileReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var mobileResp = await client.SendAsync(mobileReq);
        Assert.Equal(HttpStatusCode.Created, mobileResp.StatusCode);

        using var mobileDoc = JsonDocument.Parse(await mobileResp.Content.ReadAsStringAsync());
        var pickupCode = mobileDoc.RootElement.GetProperty("securityPickupCode").GetString();
        var expressToken = mobileDoc.RootElement.GetProperty("mobileExpressToken").GetString();
        var sessionId = mobileDoc.RootElement.GetProperty("sessionId").GetGuid();
        Assert.False(string.IsNullOrWhiteSpace(pickupCode));
        Assert.StartsWith("CHK-", expressToken, StringComparison.Ordinal);

        using var printReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/checkin/sessions/{sessionId:D}/print"
        )
        {
            Content = JsonContent.Create(new { sessionId, forceClientPrint = true }),
        };
        printReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var printResp = await client.SendAsync(printReq);
        Assert.Equal(HttpStatusCode.OK, printResp.StatusCode);
        using var printDoc = JsonDocument.Parse(await printResp.Content.ReadAsStringAsync());
        Assert.True(printDoc.RootElement.GetProperty("clientPendingCount").GetInt32() >= 2);

        using var rosterReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/checkin/rooms/roster?configurationId={configId:D}&scheduleId={scheduleId:D}"
        );
        rosterReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var rosterResp = await client.SendAsync(rosterReq);
        Assert.Equal(HttpStatusCode.OK, rosterResp.StatusCode);

        using var checkoutReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/checkin/sessions/checkout"
        )
        {
            Content = JsonContent.Create(
                new { configurationId = configId, securityPickupCode = pickupCode }
            ),
        };
        checkoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var checkoutResp = await client.SendAsync(checkoutReq);
        Assert.Equal(HttpStatusCode.OK, checkoutResp.StatusCode);
    }

    private static async Task VerifyWalletPassAndPassKitEndpointsAsync(
        HttpClient client,
        string token
    )
    {
        using var tplReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/checkin/wallet-passes/templates?activeOnly=true"
        );
        tplReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var tplResp = await client.SendAsync(tplReq);
        Assert.Equal(HttpStatusCode.OK, tplResp.StatusCode);

        using var tplDoc = JsonDocument.Parse(await tplResp.Content.ReadAsStringAsync());
        var templateId = tplDoc.RootElement[0].GetProperty("id").GetGuid();

        using var issueReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/checkin/wallet-passes/issue"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    templateId,
                    personId = Guid.CreateVersion7(),
                    dynamicFields = new
                    {
                        holderDisplayName = "Taylor Brooks",
                        membershipTier = "Gold",
                        punchOrCreditBalance = 6,
                    },
                }
            ),
        };
        issueReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var issueResp = await client.SendAsync(issueReq);
        Assert.Equal(HttpStatusCode.Created, issueResp.StatusCode);

        using var issueDoc = JsonDocument.Parse(await issueResp.Content.ReadAsStringAsync());
        var claimToken = issueDoc.RootElement.GetProperty("claimToken").GetString()!;
        var serial = issueDoc
            .RootElement.GetProperty("pass")
            .GetProperty("serialNumber")
            .GetString()!;
        var scanToken = issueDoc
            .RootElement.GetProperty("pass")
            .GetProperty("scanToken")
            .GetString()!;

        await VerifyWalletClaimScanAndPassKitStatusCodesAsync(
            client,
            token,
            claimToken,
            serial,
            scanToken
        );
    }

    private static async Task VerifyWalletClaimScanAndPassKitStatusCodesAsync(
        HttpClient client,
        string token,
        string claimToken,
        string serial,
        string scanToken
    )
    {
        using var claimResp = await client.GetAsync(
            new Uri($"/api/checkin/wallet-passes/claim/{claimToken}", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, claimResp.StatusCode);

        using var scanReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/checkin/wallet-passes/kiosk-scan"
        )
        {
            Content = JsonContent.Create(new { scanToken }),
        };
        scanReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var scanResp = await client.SendAsync(scanReq);
        Assert.Equal(HttpStatusCode.OK, scanResp.StatusCode);

        using var unauthPassKit = await client.GetAsync(
            new Uri(
                $"/api/checkin/wallet/v1/passes/pass.org.ruth.member/{serial}",
                UriKind.Relative
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unauthPassKit.StatusCode);

        using var noSerials = await client.GetAsync(
            new Uri(
                "/api/checkin/wallet/v1/devices/unknown-dev/registrations/pass.org.ruth.member",
                UriKind.Relative
            )
        );
        Assert.Equal(HttpStatusCode.NoContent, noSerials.StatusCode);
    }

    private static async Task VerifyCheckInBlazorPageAsync(HttpClient client)
    {
        foreach (
            var route in new[]
            {
                "/admin/checkin/configurations",
                "/admin/checkin/labels",
                "/checkin/kiosk",
                "/checkin/room-manager",
                "/checkin/wallet-passes",
            }
        )
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
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
}
