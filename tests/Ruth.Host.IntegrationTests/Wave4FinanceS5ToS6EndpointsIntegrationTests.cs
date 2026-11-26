using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave4FinanceS5ToS6EndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave4FinanceStrongPassword#2026!";

    [Fact]
    public async Task Statements_Assistance_Campaigns_PublicPortal_OpenApi_AndBlazorPages_WorkEndToEnd()
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
        var (accountId, gatewayId, orgPersonId) = await SeedAccountAndSponsorAsync(client, token);
        await VerifyStatementsAndAssistanceEndpointsAsync(client, token, accountId, orgPersonId);
        await VerifyCampaignAndPublicPortalEndpointsAsync(client, token, accountId, gatewayId);
        await VerifyOpenApiAndBlazorPagesAsync(client);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Finance S5-S6 Org",
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

    private static async Task<(
        Guid AccountId,
        Guid GatewayId,
        Guid OrgPersonId
    )> SeedAccountAndSponsorAsync(HttpClient client, string token)
    {
        using var accReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/accounts?activeOnly=true&isTaxDeductible=true"
        );
        accReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var accResp = await client.SendAsync(accReq);
        Assert.Equal(HttpStatusCode.OK, accResp.StatusCode);
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

        using var orgResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/organizations",
            new { name = "Horizon Grants", email = "grants@horizon.test" }
        );
        Assert.Equal(HttpStatusCode.Created, orgResp.StatusCode);
        using var orgDoc = JsonDocument.Parse(await orgResp.Content.ReadAsStringAsync());
        var orgPersonId = orgDoc.RootElement.GetProperty("organizationPersonId").GetGuid();

        using var txResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/transactions",
            new
            {
                authorizedPersonId = orgPersonId,
                currencyCode = "USD",
                sourceType = "StaffEntry",
                tenderType = "Wire",
                splits = new[] { new { accountId, amount = 450.00m } },
            }
        );
        Assert.Equal(HttpStatusCode.Created, txResp.StatusCode);

        return (accountId, gatewayId, orgPersonId);
    }

    private static async Task VerifyStatementsAndAssistanceEndpointsAsync(
        HttpClient client,
        string token,
        Guid accountId,
        Guid personId
    )
    {
        using var tmplReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/statements/templates?activeOnly=true"
        );
        tmplReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var tmplResp = await client.SendAsync(tmplReq);
        Assert.Equal(HttpStatusCode.OK, tmplResp.StatusCode);
        using var tmplDoc = JsonDocument.Parse(await tmplResp.Content.ReadAsStringAsync());
        var templateId = tmplDoc.RootElement[0].GetProperty("id").GetGuid();

        using var genResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/statements/generate",
            new
            {
                templateId,
                recipientScope = "OrganizationSponsor",
                targetPersonId = personId,
                periodStartDate = "2020-01-01",
                periodEndDate = "2030-12-31",
                persistArchiveToStorage = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, genResp.StatusCode);
        using var genDoc = JsonDocument.Parse(await genResp.Content.ReadAsStringAsync());
        var stmtId = genDoc.RootElement.GetProperty("statement").GetProperty("id").GetGuid();

        using var dlReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/finance/statements/{stmtId:D}/download?format=Pdf"
        );
        dlReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var dlResp = await client.SendAsync(dlReq);
        Assert.Equal(HttpStatusCode.OK, dlResp.StatusCode);
        Assert.Equal("application/pdf", dlResp.Content.Headers.ContentType?.MediaType);

        using var progResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/assistance/programs",
            new
            {
                name = "Host Scholarship Aid",
                code = "HOST-AID-26",
                category = "Scholarship",
                fundingAccountId = accountId,
                totalBudgetAmount = 5000.00m,
                maxAwardPerCase = 1000.00m,
            }
        );
        Assert.Equal(HttpStatusCode.Created, progResp.StatusCode);
    }

    private static async Task VerifyCampaignAndPublicPortalEndpointsAsync(
        HttpClient client,
        string token,
        Guid accountId,
        Guid gatewayId
    )
    {
        using var campResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/campaigns",
            new
            {
                name = "Host Clean Water Campaign",
                slug = "host-water-2026",
                groupId = Guid.CreateVersion7(),
                financialAccountId = accountId,
                campaignGoalAmount = 5000.00m,
                defaultParticipantGoalAmount = 500.00m,
                defaultFinancialGatewayId = gatewayId,
            }
        );
        Assert.Equal(HttpStatusCode.Created, campResp.StatusCode);

        using var pubGive = await client.PostAsJsonAsync(
            new Uri(
                "/api/finance/campaigns/public/host-water-2026/contributions",
                UriKind.Relative
            ),
            new
            {
                campaignIdOrSlug = "host-water-2026",
                amount = 150.00m,
                currencyCode = "USD",
                donorDisplayName = "Public Donor",
                donorPrivacy = "AnonymousPublicly",
                oneTimeHostedToken = "tok_public_portal_4242",
                tenderType = "CreditCard",
            }
        );
        Assert.Equal(HttpStatusCode.Created, pubGive.StatusCode);

        using var pubPortal = await client.GetAsync(
            new Uri("/api/finance/campaigns/public/host-water-2026", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, pubPortal.StatusCode);
    }

    private static async Task VerifyOpenApiAndBlazorPagesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/finance/statements/generate", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/finance/assistance/programs", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/finance/campaigns/public/{slug}",
            openApiText,
            StringComparison.Ordinal
        );

        foreach (
            var route in new[]
            {
                "/finance/statements",
                "/finance/assistance",
                "/finance/campaigns",
                "/give/campaigns/host-water-2026",
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
