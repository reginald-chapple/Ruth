using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class CrossTenantRlsCanaryIntegrationTests(HostContainersFixture containers)
{
    private const string TenantAdminPassword = "TaxExemptStrong#2026!";

    [Fact]
    public async Task StealthHosts_ForceRls_CrossTenantTokenReplay_IrsVerification_And_CustomDomain_GoLive_WorkEndToEnd()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            }
        );

        await AssertStealthUnknownHostAndSignupPlaneIsolationAsync(client);
        await CrossTenantCanaryTestHelper.AssertPostgresForceRlsOnAllTenantTablesAsync(isolatedDb);

        var (tenantAId, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Helping Hands Community Relief Inc",
            "Helping Hands",
            "helping-hands",
            "52-1234567",
            "ops@helpinghands.org",
            TenantAdminPassword
        );
        var (_, hostB) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Harbor Food Aid Foundation",
            "Harbor Food Aid",
            "harbor-aid",
            "52-7654321",
            "ops@harboraid.org",
            TenantAdminPassword
        );

        await AssertCrossTenantCrmAndCredentialIsolationAsync(client, hostA, hostB);
        await AssertCustomDomainGateAndOperatorGoLiveApprovalAsync(
            client,
            tenantAId,
            hostA,
            "giving.helpinghands.org"
        );
    }

    private static async Task AssertStealthUnknownHostAndSignupPlaneIsolationAsync(
        HttpClient client
    )
    {
        using var unknownResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                "unknown.charity.org",
                "/"
            )
        );
        Assert.Equal(HttpStatusCode.NotFound, unknownResp.StatusCode);
        Assert.Equal(string.Empty, await unknownResp.Content.ReadAsStringAsync());
        Assert.False(unknownResp.Headers.Contains("Server"));
        Assert.False(unknownResp.Headers.Contains("X-Powered-By"));

        using var unknownFrameworkAsset = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                "unknown.charity.org",
                "/_framework/blazor.web.js"
            )
        );
        Assert.Equal(HttpStatusCode.NotFound, unknownFrameworkAsset.StatusCode);

        using var signupRoot = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(HttpMethod.Get, "signup.localhost", "/")
        );
        Assert.Equal(HttpStatusCode.Redirect, signupRoot.StatusCode);
        Assert.Equal("/signup", signupRoot.Headers.Location?.OriginalString);

        using var socialClubAttempt = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                "signup.localhost",
                "/api/v1/platform/signup",
                new
                {
                    legalName = "Downtown Social Club",
                    displayName = "Social Club",
                    slug = "social-club",
                    ein = "52-9876543",
                    irsSubsectionCode = "501(c)(7)",
                    stateCode = "VA",
                    timeZoneId = "UTC",
                    primaryBranchName = "Clubhouse",
                    adminUserName = "clubadmin",
                    adminEmail = "admin@club.org",
                    adminDisplayName = "Club Admin",
                    adminPassword = TenantAdminPassword,
                }
            )
        );
        Assert.Equal(HttpStatusCode.BadRequest, socialClubAttempt.StatusCode);
    }

    private static async Task AssertCrossTenantCrmAndCredentialIsolationAsync(
        HttpClient client,
        string hostA,
        string hostB
    )
    {
        var (tokenA, cookieA) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            TenantAdminPassword
        );
        var (tokenB, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostB,
            TenantAdminPassword
        );

        // Tenant hosts hide platform /signup and /ops routes with plain 404
        using var signupOnTenant = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(HttpMethod.Get, hostA, "/signup")
        );
        using var opsOnTenant = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(HttpMethod.Get, hostA, "/ops")
        );
        Assert.Equal(HttpStatusCode.NotFound, signupOnTenant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, opsOnTenant.StatusCode);

        // Create CRM constituent inside Tenant A
        using var createPersonA = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/crm/people",
                new
                {
                    firstName = "Alice",
                    lastName = "ConfidentialDonor",
                    email = "alice@helpinghands.org",
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, createPersonA.StatusCode);

        // Replaying Tenant A's JWT or sid cookie on Tenant B's domain MUST be rejected (401)
        using var jwtReplayOnB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/auth/me",
                bearerToken: tokenA
            )
        );
        using var cookieReplayOnB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/auth/me",
                cookieHeader: cookieA
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, jwtReplayOnB.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, cookieReplayOnB.StatusCode);

        // Authenticated Tenant B admin sees ZERO records from Tenant A
        using var listPeopleB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/crm/people/search?query=ConfidentialDonor",
                bearerToken: tokenB
            )
        );
        var bodyB = await listPeopleB.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, listPeopleB.StatusCode);
        Assert.DoesNotContain("ConfidentialDonor", bodyB, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertCustomDomainGateAndOperatorGoLiveApprovalAsync(
        HttpClient client,
        Guid tenantAId,
        string sandboxHostA,
        string customDomainA
    )
    {
        var (tokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            sandboxHostA,
            TenantAdminPassword
        );

        // Register & verify custom domain while still in Sandbox
        using var regDomain = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                sandboxHostA,
                "/api/v1/tenancy/domains",
                new { hostname = customDomainA, makePrimaryWhenVerified = true },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, regDomain.StatusCode);
        using var domDoc = JsonDocument.Parse(await regDomain.Content.ReadAsStringAsync());
        var domainId = domDoc.RootElement.GetProperty("id").GetGuid();
        var txtToken = domDoc.RootElement.GetProperty("verificationTxtValue").GetString()
            ?? string.Empty;

        using var verifyDomain = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                sandboxHostA,
                $"/api/v1/tenancy/domains/{domainId:D}/verify",
                new { observedTxtToken = txtToken },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, verifyDomain.StatusCode);

        // Custom domain MUST still return empty 404 before IRS + Operator Go-Live approval
        using var preLiveCustomResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(HttpMethod.Get, customDomainA, "/login")
        );
        Assert.Equal(HttpStatusCode.NotFound, preLiveCustomResp.StatusCode);

        // Complete Mock Stripe Connect & submit for Go-Live
        using var mockStripe = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                sandboxHostA,
                "/api/v1/tenancy/stripe-connect/mock-complete",
                jsonBody: new { },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, mockStripe.StatusCode);

        using var submitGoLive = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                sandboxHostA,
                "/api/v1/tenancy/go-live/submit",
                jsonBody: new { },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, submitGoLive.StatusCode);

        await ApproveTenantOnOperatorHostAsync(client, tenantAId, sandboxHostA);

        // Custom domain is now Live, white-labeled with organization's name, zero "Ruth" branding
        using var liveLoginResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(HttpMethod.Get, customDomainA, "/login")
        );
        var liveHtml = await liveLoginResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, liveLoginResp.StatusCode);
        Assert.Contains("Helping Hands", liveHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Ruth RMS", liveHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Powered by Ruth", liveHtml, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task ApproveTenantOnOperatorHostAsync(
        HttpClient client,
        Guid tenantAId,
        string sandboxHostA
    )
    {
        using var opLogin = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                "ops.localhost",
                "/api/v1/platform/operators/authenticate",
                new { email = "compliance@ops.localhost", password = "OperatorPass!2026#" }
            )
        );
        Assert.Equal(HttpStatusCode.OK, opLogin.StatusCode);
        Assert.True(opLogin.Headers.TryGetValues("Set-Cookie", out var opCookies));
        var opCookie = opCookies
            .First(static c => c.StartsWith("sid=", StringComparison.Ordinal))
            .Split(';')[0];

        // Operator cookie MUST be rejected if replayed on a tenant workspace host
        using var opReplayOnTenant = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                sandboxHostA,
                "/api/auth/me",
                cookieHeader: opCookie
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, opReplayOnTenant.StatusCode);

        using var approveReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            "ops.localhost",
            "/api/v1/platform/operators/review",
            new
            {
                targetTenantId = tenantAId,
                decision = "ApproveLive",
                operatorEmail = "compliance@ops.localhost",
                notes = "IRS EO BMF 501(c)(3) verified.",
            },
            cookieHeader: opCookie
        );
        using var approveResp = await client.SendAsync(approveReq);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);
    }
}
