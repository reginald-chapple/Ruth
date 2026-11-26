using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave4EventsS7ToS8EndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave4EventsStrongPassword#2026!";
    private static readonly string[] _pollOptions = ["Track A", "Track B"];

    [Fact]
    public async Task Calendars_IcsFeed_InteractiveSessions_Registrations_OpenApi_AndBlazorPages_WorkEndToEnd()
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
        await VerifyCalendarAndIcsFeedEndpointsAsync(client, token);
        await VerifyInteractiveSessionEndpointsAsync(client, token);
        await VerifyRegistrationWizardEndpointsAsync(client, token);
        await VerifyOpenApiAndBlazorPagesAsync(client);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Events S7-S8 Org",
                timeZoneId = "America/Denver",
                primaryBranchName = "Central Branch",
                primaryBranchShortCode = "CENT",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task VerifyCalendarAndIcsFeedEndpointsAsync(
        HttpClient client,
        string token
    )
    {
        using var calReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/events/calendars?publicOnly=true&activeOnly=true"
        );
        calReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var calResp = await client.SendAsync(calReq);
        Assert.Equal(HttpStatusCode.OK, calResp.StatusCode);
        using var calDoc = JsonDocument.Parse(await calResp.Content.ReadAsStringAsync());
        var calendarId = calDoc.RootElement[0].GetProperty("id").GetGuid();

        using var itemResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/events/items",
            new
            {
                name = "Host Town Hall",
                slug = "host-town-hall-2026",
                calendarIds = new[] { calendarId },
            }
        );
        Assert.Equal(HttpStatusCode.Created, itemResp.StatusCode);
        using var itemDoc = JsonDocument.Parse(await itemResp.Content.ReadAsStringAsync());
        var itemId = itemDoc.RootElement.GetProperty("id").GetGuid();

        using var occResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/events/items/{itemId:D}/occurrences",
            new
            {
                locationDescription = "Auditorium",
                customStartUtc = DateTimeOffset.UtcNow.AddDays(3),
                customEndUtc = DateTimeOffset.UtcNow.AddDays(3).AddHours(2),
            }
        );
        Assert.Equal(HttpStatusCode.Created, occResp.StatusCode);

        using var icsResp = await client.GetAsync(
            new Uri("/api/events/calendars/public-events/feed.ics", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, icsResp.StatusCode);
        Assert.Equal("text/calendar", icsResp.Content.Headers.ContentType?.MediaType);
        var icsBody = await icsResp.Content.ReadAsStringAsync();
        Assert.Contains("SUMMARY:Host Town Hall", icsBody, StringComparison.Ordinal);
    }

    private static async Task VerifyInteractiveSessionEndpointsAsync(
        HttpClient client,
        string token
    )
    {
        using var createResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/events/interactive-sessions",
            new
            {
                title = "Host Live Session",
                initialActions = new[]
                {
                    new
                    {
                        title = "Track Poll",
                        promptText = "Which track?",
                        actionType = "MultipleChoicePoll",
                        options = _pollOptions,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        using var sessDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var sessionId = sessDoc.RootElement.GetProperty("id").GetGuid();
        var joinCode = sessDoc.RootElement.GetProperty("joinCode").GetString();
        var actionId = sessDoc.RootElement.GetProperty("actions")[0].GetProperty("id").GetGuid();

        using var startResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            $"/api/events/interactive-sessions/{sessionId:D}/occurrences/start",
            new { sessionId }
        );
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        using var voteResp = await client.PostAsJsonAsync(
            new Uri(
                $"/api/events/interactive-sessions/live/{joinCode}/responses",
                UriKind.Relative
            ),
            new
            {
                sessionIdOrJoinCode = joinCode,
                actionId,
                participantToken = "anon-web-1",
                selectedOption = "Track A",
            }
        );
        Assert.Equal(HttpStatusCode.Created, voteResp.StatusCode);

        using var hubNegotiate = await client.PostAsync(
            new Uri("/hubs/events/interactive/negotiate?negotiateVersion=1", UriKind.Relative),
            content: null
        );
        Assert.Equal(HttpStatusCode.OK, hubNegotiate.StatusCode);
        var negotiateJson = await hubNegotiate.Content.ReadAsStringAsync();
        Assert.Contains("connectionId", negotiateJson, StringComparison.Ordinal);
    }

    private static async Task VerifyRegistrationWizardEndpointsAsync(
        HttpClient client,
        string token
    )
    {
        using var accReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/accounts?activeOnly=true"
        );
        accReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var accResp = await client.SendAsync(accReq);
        using var accDoc = JsonDocument.Parse(await accResp.Content.ReadAsStringAsync());
        var accountId = accDoc.RootElement[0].GetProperty("id").GetGuid();

        using var gwReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/gateways?activeOnly=true"
        );
        gwReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var gwResp = await client.SendAsync(gwReq);
        using var gwDoc = JsonDocument.Parse(await gwResp.Content.ReadAsStringAsync());
        var gatewayId = gwDoc.RootElement[0].GetProperty("id").GetGuid();

        using var tmplResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/events/registrations/templates",
            new
            {
                name = "Host Conference Template",
                financialAccountId = accountId,
                defaultFinancialGatewayId = gatewayId,
                baseCostPerRegistrant = 75.00m,
                minimumInitialDepositPerRegistrant = 25.00m,
                paymentModel = "DepositAndInstallments",
            }
        );
        Assert.Equal(HttpStatusCode.Created, tmplResp.StatusCode);
        using var tmplDoc = JsonDocument.Parse(await tmplResp.Content.ReadAsStringAsync());
        var templateId = tmplDoc.RootElement.GetProperty("id").GetGuid();

        using var instResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/events/registrations/instances",
            new
            {
                templateId,
                name = "Host Conference 2026",
                slug = "host-conference-2026",
                maxAttendees = 10,
            }
        );
        Assert.Equal(HttpStatusCode.Created, instResp.StatusCode);

        using var holdResp = await client.PostAsJsonAsync(
            new Uri(
                "/api/events/registrations/public/host-conference-2026/holds",
                UriKind.Relative
            ),
            new { instanceIdOrSlug = "host-conference-2026", requestedSeatCount = 1 }
        );
        Assert.Equal(HttpStatusCode.Created, holdResp.StatusCode);
        using var holdDoc = JsonDocument.Parse(await holdResp.Content.ReadAsStringAsync());
        var sessionToken = holdDoc.RootElement.GetProperty("sessionToken").GetString();

        using var submitResp = await client.PostAsJsonAsync(
            new Uri(
                "/api/events/registrations/public/host-conference-2026/submit",
                UriKind.Relative
            ),
            new
            {
                instanceIdOrSlug = "host-conference-2026",
                registrarFirstName = "Avery",
                registrarLastName = "Brooks",
                registrarEmail = "avery@example.org",
                registrants = new[]
                {
                    new
                    {
                        firstName = "Avery",
                        lastName = "Brooks",
                        email = "avery@example.org",
                    },
                },
                sessionToken,
                initialPaymentAmount = 25.00m,
                oneTimeHostedPaymentToken = "tok_host_reg_4242",
            }
        );
        Assert.Equal(HttpStatusCode.Created, submitResp.StatusCode);
    }

    private static async Task VerifyOpenApiAndBlazorPagesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains(
            "/api/events/calendars/{slugOrId}/feed.ics",
            openApiText,
            StringComparison.Ordinal
        );
        Assert.Contains("/api/events/interactive-sessions", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/events/registrations/public/{slugOrId}/submit",
            openApiText,
            StringComparison.Ordinal
        );

        foreach (
            var route in new[]
            {
                "/events/calendars",
                "/events/interactive",
                "/events/registrations",
                "/events/register/host-conference-2026",
            }
        )
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }

    private static Task<HttpResponseMessage> SendAuthJsonAsync(
        HttpClient client,
        string token,
        HttpMethod method,
        string relativeUrl,
        object payload
    )
    {
        var request = new HttpRequestMessage(method, relativeUrl)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
