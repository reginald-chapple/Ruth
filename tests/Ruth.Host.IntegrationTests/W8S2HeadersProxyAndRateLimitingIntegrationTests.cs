using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed partial class W8S2HeadersProxyAndRateLimitingIntegrationTests(
    HostContainersFixture containers
) : IDisposable
{
    private const string AdminPassword = "TaxExemptStrong#2026!";

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task SecurityHeaders_CspNonce_ZeroFingerprints_AndProductionOpenApiLockdown_AreEnforced()
    {
        await using var devFactory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Development"
        );
        using var devClient = devFactory.CreateNoRedirectClient();

        using var firstResp = await devClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, new Uri("https://localhost/"))
        );
        using var secondResp = await devClient.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, new Uri("https://localhost/"))
        );

        Assert.Equal(HttpStatusCode.OK, firstResp.StatusCode);
        var csp1 = AssertSecurityAndZeroFingerprintHeaders(firstResp);
        var csp2 = AssertSecurityAndZeroFingerprintHeaders(secondResp);

        var nonce1 = ExtractCspNonce(csp1);
        var nonce2 = ExtractCspNonce(csp2);
        Assert.NotEqual(nonce1, nonce2, StringComparer.Ordinal);

        var html = await firstResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Powered by Ruth", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Swashbuckle", html, StringComparison.OrdinalIgnoreCase);
        if (html.Contains("<script type=\"importmap\"", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Contains($"nonce=\"{nonce1}\"", html, StringComparison.Ordinal);
        }

        await AssertProductionOpenApiLockedDownEvenWhenEnabledFlagTrueAsync();
    }

    [Fact]
    public async Task UntrustedProxyAndHostSpoofing_AreRejected_WhileTrustedProxyCidrIsHonored()
    {
        await using var untrustedFactory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Development"
        );
        using var untrustedClient = untrustedFactory.CreateNoRedirectClient();

        await AssertUntrustedHeaderRejectedAsync(
            untrustedClient,
            "X-Forwarded-For",
            "203.0.113.44"
        );
        await AssertUntrustedHeaderRejectedAsync(
            untrustedClient,
            "X-Forwarded-Host",
            "evil.attacker.example"
        );
        await AssertUntrustedHeaderRejectedAsync(untrustedClient, "X-Original-URL", "/ops");
        await AssertMalformedAuthorityRejectedAsync(untrustedClient);

        await AssertTrustedProxyHonorsXForwardedForPerIpPartitioningAsync();
    }

    [Fact]
    public async Task CanonicalTenantUrls_AndTieredPerTenantPerIpRateLimiting_EnforceStrictBoundaries()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        var overrides = BuildMultiTenantRateLimitOverrides();
        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: overrides
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (tenantAId, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Aurora Aid Foundation",
            "Aurora Aid",
            "aurora-aid",
            "52-7711223",
            "ops@auroraaid.org",
            AdminPassword
        );
        var (_, hostB) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Beacon Aid Foundation",
            "Beacon Aid",
            "beacon-aid",
            "52-7711224",
            "ops@beaconaid.org",
            AdminPassword
        );

        await VerifyCanonicalUrlResolutionAsync(factory.Services, tenantAId, hostA);
        await VerifyAuthAndKioskBruteForceThrottlingWithTenantIsolationAsync(client, hostA, hostB);
    }

    private static string AssertSecurityAndZeroFingerprintHeaders(HttpResponseMessage response)
    {
        string[] forbiddenHeaders =
        [
            "Server",
            "X-Powered-By",
            "X-AspNet-Version",
            "X-AspNetMvc-Version",
            "X-AspNetCore-Version",
            "X-SourceFiles",
            "X-Generator",
            "X-Runtime",
        ];
        foreach (var header in forbiddenHeaders)
        {
            Assert.False(response.Headers.Contains(header), $"Forbidden header leaked: {header}");
        }

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal(
            "strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").Single()
        );
        Assert.Equal(
            "same-origin",
            response.Headers.GetValues("Cross-Origin-Opener-Policy").Single()
        );
        Assert.Equal(
            "same-origin",
            response.Headers.GetValues("Cross-Origin-Resource-Policy").Single()
        );
        Assert.Equal(
            "none",
            response.Headers.GetValues("X-Permitted-Cross-Domain-Policies").Single()
        );

        var permissions = response.Headers.GetValues("Permissions-Policy").Single();
        Assert.Contains("camera=()", permissions, StringComparison.Ordinal);
        Assert.Contains("microphone=()", permissions, StringComparison.Ordinal);
        Assert.Contains("geolocation=()", permissions, StringComparison.Ordinal);

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("object-src 'none'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("form-action 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("upgrade-insecure-requests", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("'unsafe-eval'", csp, StringComparison.Ordinal);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp, StringComparison.Ordinal);
        return csp;
    }

    private static string ExtractCspNonce(string csp)
    {
        var match = NonceRegex.Match(csp);
        Assert.True(match.Success, $"Expected cryptographic script nonce in CSP: {csp}");
        return match.Groups["nonce"].Value;
    }

    private static async Task AssertProductionOpenApiLockedDownEvenWhenEnabledFlagTrueAsync()
    {
        var prodOverrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OpenApi:Enabled"] = "true",
        };
        await using var prodFactory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Production",
            overrides: prodOverrides
        );
        using var prodClient = prodFactory.CreateNoRedirectClient();
        using var openApiResp = await prodClient.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.NotFound, openApiResp.StatusCode);
    }

    private static async Task AssertUntrustedHeaderRejectedAsync(
        HttpClient client,
        string headerName,
        string headerValue
    )
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        _ = req.Headers.TryAddWithoutValidation(headerName, headerValue);
        using var resp = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Contains(
            "application/problem+json",
            resp.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static async Task AssertMalformedAuthorityRejectedAsync(HttpClient client)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        _ = req.Headers.Remove("Host");
        _ = req.Headers.TryAddWithoutValidation("Host", "tenant.sandbox.localhost@evil.example");
        using var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    private static async Task AssertTrustedProxyHonorsXForwardedForPerIpPartitioningAsync()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ProxyTrust:TrustedProxyCidrs:0"] = "10.10.0.0/16",
            ["RateLimiting:PermitLimit"] = "1",
            ["RateLimiting:WindowSeconds"] = "60",
        };
        await using var proxyFactory = HostWebApplicationFactory.CreateWithOfflineDependencies(
            "Development",
            overrides: overrides,
            configureServices: static services =>
                services.Insert(
                    0,
                    ServiceDescriptor.Singleton<IStartupFilter>(
                        new PrePipelinePeerIpStartupFilter(IPAddress.Parse("10.10.0.5"))
                    )
                )
        );
        using var proxyClient = proxyFactory.CreateNoRedirectClient();

        using var ip1First = CreateForwardedGet("/openapi/v1.json", "198.51.100.10");
        using var ip1Second = CreateForwardedGet("/openapi/v1.json", "198.51.100.10");
        using var ip2First = CreateForwardedGet("/openapi/v1.json", "198.51.100.11");

        Assert.Equal(HttpStatusCode.OK, (await proxyClient.SendAsync(ip1First)).StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await proxyClient.SendAsync(ip1Second)).StatusCode
        );
        Assert.Equal(HttpStatusCode.OK, (await proxyClient.SendAsync(ip2First)).StatusCode);

        using var hostOverrideAttempt = CreateForwardedGet("/openapi/v1.json", "198.51.100.12");
        _ = hostOverrideAttempt.Headers.TryAddWithoutValidation("X-Forwarded-Host", "evil.example");
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await proxyClient.SendAsync(hostOverrideAttempt)).StatusCode
        );
    }

    private static HttpRequestMessage CreateForwardedGet(string path, string clientIp)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        _ = req.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);
        _ = req.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        return req;
    }

    private static Dictionary<string, string?> BuildMultiTenantRateLimitOverrides()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
            ["Tenancy:DedicatedWorkspaceTenantId"] = string.Empty,
            ["Tenancy:SandboxBaseDomain"] = "sandbox.localhost",
            ["Tenancy:PlatformOperatorHost"] = "ops.localhost",
            ["Tenancy:SeedOperatorEmail"] = "compliance@ops.localhost",
            ["Tenancy:SeedOperatorPassword"] = "OperatorPass!2026#",
            ["Tenancy:StripeMockMode"] = "true",
            ["RateLimiting:AuthPermitLimit"] = "3",
            ["RateLimiting:AuthWindowSeconds"] = "60",
            ["RateLimiting:KioskPermitLimit"] = "3",
            ["RateLimiting:KioskWindowSeconds"] = "60",
        };
    }

    private static async Task VerifyCanonicalUrlResolutionAsync(
        IServiceProvider services,
        Guid tenantAId,
        string expectedHostA
    )
    {
        var urlProvider = services.GetRequiredService<ICanonicalTenantUrlProvider>();
        var resetUrl = await urlProvider.BuildTenantUrlAsync(
            TenantId.From(tenantAId),
            "/reset-password?token=tok_w8s2"
        );
        var rsvpUrl = await urlProvider.BuildTenantUrlAsync(
            TenantId.From(tenantAId),
            "/api/groups/scheduling/rsvp/rsvp_tok_1"
        );

        Assert.Equal($"https://{expectedHostA}/reset-password?token=tok_w8s2", resetUrl);
        Assert.Equal($"https://{expectedHostA}/api/groups/scheduling/rsvp/rsvp_tok_1", rsvpUrl);
    }

    private static async Task VerifyAuthAndKioskBruteForceThrottlingWithTenantIsolationAsync(
        HttpClient client,
        string hostA,
        string hostB
    )
    {
        for (var i = 0; i < 3; i++)
        {
            using var attempt = await SendBadLoginAsync(client, hostA);
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
        }

        using var fourthAuth = await SendBadLoginAsync(client, hostA);
        await AssertProblem429WithRetryAfterAsync(fourthAuth);

        // Tenant B's auth quota is isolated from Tenant A's exhausted quota
        using var tenantBAttempt = await SendBadLoginAsync(client, hostB);
        Assert.Equal(HttpStatusCode.Unauthorized, tenantBAttempt.StatusCode);

        // Kiosk / RSVP token enumeration burst on Tenant A
        for (var i = 0; i < 3; i++)
        {
            using var rsvpProbe = await client.SendAsync(
                CrossTenantCanaryTestHelper.CreateHostRequest(
                    HttpMethod.Get,
                    hostA,
                    "/api/groups/scheduling/rsvp/nonexistent-rsvp-token"
                )
            );
            Assert.Equal(HttpStatusCode.NotFound, rsvpProbe.StatusCode);
        }

        using var fourthRsvp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/groups/scheduling/rsvp/nonexistent-rsvp-token"
            )
        );
        await AssertProblem429WithRetryAfterAsync(fourthRsvp);
    }

    private static Task<HttpResponseMessage> SendBadLoginAsync(HttpClient client, string host)
    {
        return client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/login",
                new { userNameOrEmail = "unknown-user", password = "WrongPassword#123!" }
            )
        );
    }

    private static async Task AssertProblem429WithRetryAfterAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, doc.RootElement.GetProperty("status").GetInt32());
    }

    [GeneratedRegex(
        """script-src 'self' 'nonce-(?<nonce>[A-Za-z0-9+/=]+)'""",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000
    )]
    private static partial Regex NonceRegex { get; }

    private sealed class PrePipelinePeerIpStartupFilter(IPAddress peerIp) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                _ = app.Use(
                    (ctx, nextMiddleware) =>
                    {
                        ctx.Connection.RemoteIpAddress = peerIp;
                        return nextMiddleware();
                    }
                );
                next(app);
            };
        }
    }
}
