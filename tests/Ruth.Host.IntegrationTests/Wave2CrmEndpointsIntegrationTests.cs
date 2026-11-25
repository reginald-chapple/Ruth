using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave2CrmEndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave2StrongPassword#2026!";

    [Fact]
    public async Task CrmEndpoints_AuthProtection_OpenApi_AndWebhookHmac_WorkEndToEnd()
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
        var requestId = await RequestBackgroundCheckAsync(client, token, personId);
        await VerifyWebhookHmacAndOpenApiAsync(client, requestId);
        await VerifyCrmBlazorPagesAsync(client, personId);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth CRM Test Church",
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

        using var unauth = await client.GetAsync(
            new Uri("/api/crm/people/search", UriKind.Relative)
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

    private static async Task<Guid> CreatePersonAsync(HttpClient client, string token)
    {
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/crm/people")
        {
            Content = JsonContent.Create(
                new
                {
                    firstName = "Claire",
                    lastName = "Bennett",
                    email = "claire@ruth.test",
                    connectionStatus = "Member",
                }
            ),
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var createResp = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        using var personDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        return personDoc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> RequestBackgroundCheckAsync(
        HttpClient client,
        string token,
        Guid personId
    )
    {
        using var checkReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/crm/background-checks/request"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    personId,
                    providerKey = InMemoryBackgroundCheckProvider.Key,
                    packageName = "standard-volunteer",
                }
            ),
        };
        checkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var checkResp = await client.SendAsync(checkReq);
        Assert.Equal(HttpStatusCode.Created, checkResp.StatusCode);
        using var checkDoc = JsonDocument.Parse(await checkResp.Content.ReadAsStringAsync());
        return checkDoc.RootElement.GetProperty("requestId").GetString()!;
    }

    private static async Task VerifyWebhookHmacAndOpenApiAsync(HttpClient client, string requestId)
    {
        var webhookPayload =
            $$"""{"requestId":"{{requestId}}","status":"clear","responseId":"doc_99"}""";

        using var badWebhook = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/crm/webhooks/background-checks/in-memory"
        )
        {
            Content = new StringContent(webhookPayload, Encoding.UTF8, "application/json"),
        };
        badWebhook.Headers.Add("X-Ruth-Signature", "0000invalid");
        using var badWebhookResp = await client.SendAsync(badWebhook);
        Assert.Equal(HttpStatusCode.Unauthorized, badWebhookResp.StatusCode);

        using var goodWebhook = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/crm/webhooks/background-checks/in-memory"
        )
        {
            Content = new StringContent(webhookPayload, Encoding.UTF8, "application/json"),
        };
        goodWebhook.Headers.Add(
            "X-Ruth-Signature",
            InMemoryBackgroundCheckProvider.ComputeSignature(webhookPayload)
        );
        using var goodWebhookResp = await client.SendAsync(goodWebhook);
        Assert.Equal(HttpStatusCode.OK, goodWebhookResp.StatusCode);

        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/crm/people", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/crm/households", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/crm/duplicates/merge", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/crm/webhooks/background-checks/{providerKey}",
            openApiText,
            StringComparison.Ordinal
        );
    }

    private static async Task VerifyCrmBlazorPagesAsync(HttpClient client, Guid personId)
    {
        var routes = new[]
        {
            "/crm/people",
            $"/crm/people/{personId:D}",
            "/crm/duplicates",
            "/crm/merge",
            "/admin/crm/settings",
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
