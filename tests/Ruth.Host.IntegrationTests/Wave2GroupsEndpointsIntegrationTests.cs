using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave2GroupsEndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave2GroupsStrongPassword#2026!";

    [Fact]
    public async Task GroupsEndpoints_AuthProtection_PostGisFinder_Rsvp_Attendance_OpenApi_AndBlazorPages_WorkEndToEnd()
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

        var token = await OnboardAndLoginAsync(client);
        var personId = await CreatePersonAsync(client, token);
        var (groupTypeId, groupId, locationId) = await CreateGroupWithLocationAsync(client, token);
        await VerifyPostGisFinderAndSimpleRegisterAsync(
            client,
            token,
            groupTypeId,
            groupId,
            personId
        );
        await VerifyAnonymousRsvpAndAttendanceApiAsync(
            client,
            token,
            groupId,
            locationId,
            personId
        );
        await VerifyOpenApiContainsGroupsRoutesAsync(client);
        await VerifyGroupsBlazorPagesAsync(client, groupId);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Groups Test Church",
                timeZoneId = "America/Denver",
                primaryCampusName = "Main Campus",
                primaryCampusShortCode = "MAIN",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var unauth = await client.GetAsync(new Uri("/api/groups/types", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<Guid> CreatePersonAsync(HttpClient client, string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/crm/people")
        {
            Content = JsonContent.Create(
                new
                {
                    firstName = "Hannah",
                    lastName = "Brooks",
                    email = "hannah@ruth.test",
                    connectionStatus = "Member",
                }
            ),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<(
        Guid GroupTypeId,
        Guid GroupId,
        Guid LocationId
    )> CreateGroupWithLocationAsync(HttpClient client, string token)
    {
        using var typeReq = new HttpRequestMessage(HttpMethod.Post, "/api/groups/types")
        {
            Content = JsonContent.Create(
                new
                {
                    name = "Serving Small Group",
                    groupTerm = "Group",
                    groupMemberTerm = "Member",
                    takesAttendance = true,
                    attendanceCountsAsWeekendService = true,
                    isSchedulingEnabled = true,
                    enableLocationSchedules = true,
                    initialRoles = new[]
                    {
                        new
                        {
                            name = "Member",
                            isLeader = false,
                            canView = true,
                        },
                    },
                }
            ),
        };
        typeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var typeResp = await client.SendAsync(typeReq);
        Assert.Equal(HttpStatusCode.Created, typeResp.StatusCode);
        using var typeDoc = JsonDocument.Parse(await typeResp.Content.ReadAsStringAsync());
        var groupTypeId = typeDoc.RootElement.GetProperty("id").GetGuid();

        using var groupReq = new HttpRequestMessage(HttpMethod.Post, "/api/groups")
        {
            Content = JsonContent.Create(
                new
                {
                    groupTypeId,
                    name = "Denver West Group",
                    isActive = true,
                    isPublic = true,
                    groupCapacity = 15,
                }
            ),
        };
        groupReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var groupResp = await client.SendAsync(groupReq);
        Assert.Equal(HttpStatusCode.Created, groupResp.StatusCode);
        using var groupDoc = JsonDocument.Parse(await groupResp.Content.ReadAsStringAsync());
        var groupId = groupDoc.RootElement.GetProperty("id").GetGuid();

        var locationId = Guid.CreateVersion7();
        using var locReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/groups/{groupId:D}/locations"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    groupId,
                    locationId,
                    locationName = "Westside Hub",
                    geoPoint = new { latitude = 39.7392, longitude = -104.9903 },
                }
            ),
        };
        locReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var locResp = await client.SendAsync(locReq);
        Assert.Equal(HttpStatusCode.OK, locResp.StatusCode);

        return (groupTypeId, groupId, locationId);
    }

    private static async Task VerifyPostGisFinderAndSimpleRegisterAsync(
        HttpClient client,
        string token,
        Guid groupTypeId,
        Guid groupId,
        Guid personId
    )
    {
        using var finderReq = new HttpRequestMessage(HttpMethod.Post, "/api/groups/finder")
        {
            Content = JsonContent.Create(
                new
                {
                    groupTypeId,
                    latitude = 39.7400,
                    longitude = -104.9900,
                    radiusMeters = 10000.0,
                    publicOnly = true,
                    activeOnly = true,
                    hasOpenCapacityOnly = true,
                }
            ),
        };
        finderReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var finderResp = await client.SendAsync(finderReq);
        Assert.Equal(HttpStatusCode.OK, finderResp.StatusCode);
        using var finderDoc = JsonDocument.Parse(await finderResp.Content.ReadAsStringAsync());
        Assert.Equal(1, finderDoc.RootElement.GetArrayLength());
        Assert.Equal(groupId, finderDoc.RootElement[0].GetProperty("groupId").GetGuid());

        using var regReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/groups/{groupId:D}/simple-register"
        )
        {
            Content = JsonContent.Create(new { groupId, personId }),
        };
        regReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var regResp = await client.SendAsync(regReq);
        Assert.Equal(HttpStatusCode.Created, regResp.StatusCode);
    }

    private static async Task VerifyAnonymousRsvpAndAttendanceApiAsync(
        HttpClient client,
        string token,
        Guid groupId,
        Guid locationId,
        Guid personId
    )
    {
        using var schedReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/groups/scheduling/schedule-volunteer"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    groupId,
                    personId,
                    occurrenceDate = "2026-09-06",
                    locationId,
                }
            ),
        };
        schedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var schedResp = await client.SendAsync(schedReq);
        Assert.Equal(HttpStatusCode.Created, schedResp.StatusCode);
        using var schedDoc = JsonDocument.Parse(await schedResp.Content.ReadAsStringAsync());
        var rsvpToken = schedDoc.RootElement.GetProperty("rsvpToken").GetString()
            ?? throw new InvalidOperationException("Missing rsvpToken.");

        // Anonymous GET & POST RSVP endpoints work without Bearer token
        using var getRsvp = await client.GetAsync(
            new Uri($"/api/groups/scheduling/rsvp/{rsvpToken}", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, getRsvp.StatusCode);

        using var respondRsvp = await client.PostAsJsonAsync(
            new Uri("/api/groups/scheduling/rsvp/respond", UriKind.Relative),
            new { rsvpToken, response = 1 }
        );
        Assert.Equal(HttpStatusCode.OK, respondRsvp.StatusCode);

        using var attReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/groups/attendance/groups/{groupId:D}/record"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    groupId,
                    occurrenceDate = "2026-09-06",
                    locationId,
                    anonymousAttendanceCount = 4,
                    entries = new[] { new { personId, didAttend = true } },
                }
            ),
        };
        attReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var attResp = await client.SendAsync(attReq);
        Assert.Equal(HttpStatusCode.OK, attResp.StatusCode);

        using var metricsReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/groups/attendance/metrics?startDate=2026-09-01&endDate=2026-09-30"
        );
        metricsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var metricsResp = await client.SendAsync(metricsReq);
        Assert.Equal(HttpStatusCode.OK, metricsResp.StatusCode);
        using var metricsDoc = JsonDocument.Parse(await metricsResp.Content.ReadAsStringAsync());
        Assert.Equal(5, metricsDoc.RootElement.GetProperty("totalAttendance").GetInt32());
    }

    private static async Task VerifyOpenApiContainsGroupsRoutesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();

        Assert.Contains("/api/groups/types", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/groups/finder", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/groups/requirements/types", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/groups/sync/run", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/groups/scheduling/rsvp/{token}",
            openApiText,
            StringComparison.Ordinal
        );
        Assert.Contains("/api/groups/attendance/metrics", openApiText, StringComparison.Ordinal);
    }

    private static async Task VerifyGroupsBlazorPagesAsync(HttpClient client, Guid groupId)
    {
        var routes = new[]
        {
            "/admin/groups/types",
            "/groups",
            $"/groups/{groupId:D}",
            "/groups/finder",
            "/groups/placement",
            "/admin/groups/requirement-types",
            "/groups/scheduler",
            "/groups/toolbox",
            $"/groups/{groupId:D}/attendance",
        };
        var forbiddenMarkers = new[]
        {
            "FluentTextField",
            "fluent-text-field",
            "FluentDesignTheme",
            "FluentNavMenu",
            "Appearance.Accent",
            "An unhandled error has occurred",
        };

        foreach (var route in routes)
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            foreach (var marker in forbiddenMarkers)
            {
                Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
