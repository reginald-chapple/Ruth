using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Host.Middleware;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class W7S7StealthCookieAndCircuitIsolationTests(HostContainersFixture containers)
{
    private const string TenantAdminPassword = "TaxExemptStrong#2026!";

    [Fact]
    public async Task HttpsHostPrefixedCookies_CookieTossingRejection_StealthHeaders_AndZeroBranding_WorkEndToEnd()
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

        var (_, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Cascade Aid Society",
            "Cascade Aid",
            "cascade-aid",
            "52-2223334",
            "ops@cascadeaid.org",
            TenantAdminPassword
        );

        await VerifyHttpsHostSidCookieAndTossingDefenseAsync(client, hostA);
        await VerifyHttpsHostCsrfCookieUpgradeAndPlainRejectionAsync(factory.Services);
        await VerifyWhiteLabelPagesAndHeadersContainZeroRuthBrandingAsync(client, hostA);
    }

    [Fact]
    public async Task BlazorCircuitBinding_CrossPlaneRouter_AndSignalRHubFilter_EnforceImmutableTenantIsolation()
    {
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        await W7S7CircuitAndSignalRTestHelper.VerifyCircuitImmutabilityAndPrincipalValidationAsync(
            tenantA,
            tenantB
        );
        await W7S7CircuitAndSignalRTestHelper.VerifyCrossPlaneBlazorRouterIsolationAsync(tenantA);
        await W7S7SignalRFilterTestHelper.VerifySignalRHubFilterEnforcesHandshakeTenantAsync(
            tenantA,
            tenantB
        );
    }

    private static async Task VerifyHttpsHostSidCookieAndTossingDefenseAsync(
        HttpClient client,
        string hostA
    )
    {
        using var httpsLogin = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/auth/cookie-login",
                new
                {
                    userNameOrEmail = "orgadmin",
                    password = TenantAdminPassword,
                    rememberMe = false,
                },
                useHttps: true
            )
        );
        Assert.Equal(HttpStatusCode.OK, httpsLogin.StatusCode);
        Assert.True(httpsLogin.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var rawHostCookie = Assert.Single(setCookies, static c =>
            c.StartsWith("__Host-sid=", StringComparison.Ordinal)
        );
        Assert.Contains("path=/", rawHostCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", rawHostCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", rawHostCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", rawHostCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ruth", rawHostCookie, StringComparison.OrdinalIgnoreCase);

        var hostCookiePair = rawHostCookie.Split(';')[0];
        var cookieValueOnly = hostCookiePair["__Host-sid=".Length..];

        using var validMe = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/auth/me",
                cookieHeader: hostCookiePair,
                useHttps: true
            )
        );
        Assert.Equal(HttpStatusCode.OK, validMe.StatusCode);

        // Subdomain cookie-tossing attempt sending unprefixed sid=<validValue> over HTTPS MUST fail (401)
        using var tossedSidAttempt = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/auth/me",
                cookieHeader: $"sid={cookieValueOnly}",
                useHttps: true
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, tossedSidAttempt.StatusCode);

        // Replaying tenant __Host-sid cookie onto Platform Operator host MUST be rejected (401)
        using var tenantOnOpsAttempt = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                "ops.localhost",
                "/api/v1/platform/operators/queue",
                cookieHeader: hostCookiePair,
                useHttps: true
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, tenantOnOpsAttempt.StatusCode);
    }

    private static async Task VerifyHttpsHostCsrfCookieUpgradeAndPlainRejectionAsync(
        IServiceProvider services
    )
    {
        var antiforgery = services.GetRequiredService<IAntiforgery>();
        var middleware = new StealthResponseHeadersMiddleware(ctx =>
        {
            var tokens = antiforgery.GetAndStoreTokens(ctx);
            ctx.Response.Headers["X-Test-Request-Token"] = tokens.RequestToken;
            return Task.CompletedTask;
        });

        var issueCtx = new DefaultHttpContext { RequestServices = services };
        issueCtx.Request.Scheme = "https";
        issueCtx.Request.Host = new HostString("cascade-aid.sandbox.localhost");
        await middleware.InvokeAsync(issueCtx);

        var verifyInbound = new StealthResponseHeadersMiddleware(ctx =>
        {
            Assert.Equal("promoted-token", ctx.Request.Cookies["csrf"]);
            return Task.CompletedTask;
        });
        var inboundCtx = new DefaultHttpContext { RequestServices = services };
        inboundCtx.Request.Scheme = "https";
        inboundCtx.Request.Headers.Cookie = "csrf=evil-tossed; __Host-csrf=promoted-token";
        await verifyInbound.InvokeAsync(inboundCtx);
    }

    private static async Task VerifyWhiteLabelPagesAndHeadersContainZeroRuthBrandingAsync(
        HttpClient client,
        string hostA
    )
    {
        (string Host, string Path)[] targets =
        [
            (hostA, "/"),
            (hostA, "/login"),
            (hostA, "/lms/catalog"),
            ("ops.localhost", "/ops"),
            ("signup.localhost", "/signup"),
        ];

        foreach (var (host, path) in targets)
        {
            using var resp = await client.SendAsync(
                CrossTenantCanaryTestHelper.CreateHostRequest(
                    HttpMethod.Get,
                    host,
                    path,
                    useHttps: true
                )
            );
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.False(resp.Headers.Contains("Server"));
            Assert.False(resp.Headers.Contains("X-Powered-By"));
            Assert.False(resp.Headers.Contains("X-AspNet-Version"));
            Assert.Equal("nosniff", resp.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("SAMEORIGIN", resp.Headers.GetValues("X-Frame-Options").Single());

            if (resp.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var c in cookies)
                {
                    if (c.Contains("csrf", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.StartsWith("__Host-csrf=", c, StringComparison.Ordinal);
                        Assert.Contains("secure", c, StringComparison.OrdinalIgnoreCase);
                        Assert.DoesNotContain("domain=", c, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            var html = await resp.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Powered by Ruth", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Ruth RMS", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Ruth CMS", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Ruth LMS", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("— Ruth", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
