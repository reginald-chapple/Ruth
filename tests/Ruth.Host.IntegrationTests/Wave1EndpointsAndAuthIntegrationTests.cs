using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave1EndpointsAndAuthIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave1StrongPassword#2026!";

    private static readonly string[] _forbiddenV4Markers =
    [
        "FluentTextField",
        "fluent-text-field",
        "FluentDesignTheme",
        "FluentNavMenu",
        "Appearance.Accent",
        "An unhandled error has occurred",
    ];

    [Fact]
    public async Task Auth_JwtCookieAndApiKeySchemes_WorkEndToEndAndEnforceUnauthorized()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateMigratedFactory(isolatedDb);
        using var client = factory.CreateNoRedirectClient();

        await CompleteOnboardingAsync(client);

        // Unauthenticated requests to protected endpoints return 401
        using var unauthRoles = await client.GetAsync(
            new Uri("/api/security/roles", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unauthRoles.StatusCode);

        // 1. JWT Bearer login + /api/auth/me
        var (accessToken, refreshToken) = await LoginForJwtAsync(client, "admin", AdminPassword);
        using var meReq = CreateBearerGet("/api/auth/me", accessToken);
        using var meResp = await client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
        using var meDoc = JsonDocument.Parse(await meResp.Content.ReadAsStringAsync());
        Assert.Equal("admin", meDoc.RootElement.GetProperty("userName").GetString());

        // Rotating refresh token succeeds once, and reusing rotated token returns 401
        using var refresh1 = await client.PostAsJsonAsync(
            new Uri("/api/auth/refresh", UriKind.Relative),
            new { refreshToken }
        );
        Assert.Equal(HttpStatusCode.OK, refresh1.StatusCode);

        using var refreshReplay = await client.PostAsJsonAsync(
            new Uri("/api/auth/refresh", UriKind.Relative),
            new { refreshToken }
        );
        Assert.Equal(HttpStatusCode.Unauthorized, refreshReplay.StatusCode);

        // 2. REST API Key creation + Authorization-Token header + revocation
        await VerifyApiKeyLifecycleAsync(client, accessToken);

        // 3. Cookie login sets .Ruth.Auth cookie + works on /api/auth/me + logout clears it
        await VerifyCookieAuthLifecycleAsync(client);
    }

    [Fact]
    public async Task SecurityAndCoreMinimalApis_ReturnExpectedPayloadsAndEnforceAdminRole()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateMigratedFactory(isolatedDb);
        using var client = factory.CreateNoRedirectClient();

        await CompleteOnboardingAsync(client);
        var (adminToken, _) = await LoginForJwtAsync(client, "admin", AdminPassword);

        // Provision a non-admin user and verify 403 Forbidden on admin endpoint
        using var createUserReq = CreateBearerPost(
            "/api/auth/users",
            adminToken,
            new
            {
                userName = "member1",
                email = "member1@grace.org",
                displayName = "Regular Member",
                password = "MemberPassword#2026!",
                isConfirmed = true,
            }
        );
        using var createUserResp = await client.SendAsync(createUserReq);
        Assert.Equal(HttpStatusCode.Created, createUserResp.StatusCode);

        var (memberToken, _) = await LoginForJwtAsync(client, "member1", "MemberPassword#2026!");
        using var memberRolesReq = CreateBearerGet("/api/security/roles", memberToken);
        using var memberRolesResp = await client.SendAsync(memberRolesReq);
        Assert.Equal(HttpStatusCode.Forbidden, memberRolesResp.StatusCode);

        // Admin security endpoints: roles, verify, audit logs
        await AssertJsonArrayGetAsync(client, "/api/security/roles", adminToken, minCount: 1);
        using var verifyReq = CreateBearerGet(
            "/api/security/verify?entityTypeKey=Ruth.Core.DefinedType&action=View",
            adminToken
        );
        using var verifyResp = await client.SendAsync(verifyReq);
        Assert.Equal(HttpStatusCode.OK, verifyResp.StatusCode);

        // Core endpoints: entity-types, 25 field-types, defined-types + JSONB attribute query
        await AssertJsonArrayGetAsync(client, "/api/core/entity-types", adminToken, minCount: 6);
        await AssertJsonArrayGetAsync(client, "/api/core/field-types", adminToken, minCount: 25);
        await VerifyCoreDefinedTypeAndCategoryApisAsync(client, adminToken);
    }

    [Fact]
    public async Task Wave1BlazorPages_RenderSsrHtmlWithFluentUiV5AndNoErrors()
    {
        var routes = new[]
        {
            "/login",
            "/change-password",
            "/security/login-history",
            "/admin/security/roles",
            "/admin/security/api-keys",
            "/admin/security/verify",
            "/admin/security/audit-log",
            "/admin/core/defined-types",
            "/admin/core/categories",
            "/admin/core/attributes",
            "/admin/core/attribute-matrices",
            "/admin/core/branches",
        };

        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateMigratedFactory(isolatedDb);
        using var client = factory.CreateNoRedirectClient();

        await CompleteOnboardingAsync(client);

        foreach (var route in routes)
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            foreach (var marker in _forbiddenV4Markers)
            {
                Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private HostWebApplicationFactory CreateMigratedFactory(string isolatedDbConnection)
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
        };
        return HostWebApplicationFactory.Create(
            isolatedDbConnection,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: overrides
        );
    }

    private static async Task CompleteOnboardingAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Civic Foundation",
                timeZoneId = "UTC",
                primaryBranchName = "Main Branch",
                primaryBranchShortCode = "MAIN",
                adminUserName = "admin",
                adminEmail = "admin@civic.org",
                adminDisplayName = "Civic Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<(string AccessToken, string RefreshToken)> LoginForJwtAsync(
        HttpClient client,
        string userNameOrEmail,
        string password
    )
    {
        using var resp = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail, password }
        );
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return (
            doc.RootElement.GetProperty("accessToken").GetString()!,
            doc.RootElement.GetProperty("refreshToken").GetString()!
        );
    }

    private static async Task VerifyApiKeyLifecycleAsync(HttpClient client, string accessToken)
    {
        using var createReq = CreateBearerPost(
            "/api/security/api-keys",
            accessToken,
            new { name = "Integration Test Key", description = "Automated key" }
        );
        using var createResp = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        using var keyDoc = JsonDocument.Parse(await createResp.Content.ReadAsStringAsync());
        var keyId = keyDoc.RootElement.GetProperty("key").GetProperty("id").GetGuid();
        var rawKey = keyDoc.RootElement.GetProperty("plaintextApiKey").GetString() ?? string.Empty;

        using var apiKeyMeReq = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/api/auth/me", UriKind.Relative)
        );
        apiKeyMeReq.Headers.Add("Authorization-Token", rawKey);
        using var apiKeyMeResp = await client.SendAsync(apiKeyMeReq);
        Assert.Equal(HttpStatusCode.OK, apiKeyMeResp.StatusCode);

        using var revokeReq = new HttpRequestMessage(
            HttpMethod.Delete,
            new Uri($"/api/security/api-keys/{keyId:D}", UriKind.Relative)
        );
        revokeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var revokeResp = await client.SendAsync(revokeReq);
        Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

        using var revokedMeReq = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/api/auth/me", UriKind.Relative)
        );
        revokedMeReq.Headers.Add("Authorization-Token", rawKey);
        using var revokedMeResp = await client.SendAsync(revokedMeReq);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedMeResp.StatusCode);
    }

    private static async Task VerifyCookieAuthLifecycleAsync(HttpClient client)
    {
        using var loginResp = await client.PostAsJsonAsync(
            new Uri("/api/auth/cookie-login", UriKind.Relative),
            new
            {
                userNameOrEmail = "admin",
                password = AdminPassword,
                rememberMe = false,
            }
        );
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        Assert.True(loginResp.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var authCookie = setCookies
            .First(static c => c.StartsWith(".Ruth.Auth=", StringComparison.Ordinal))
            .Split(';')[0];

        using var cookieMeReq = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri("/api/auth/me", UriKind.Relative)
        );
        cookieMeReq.Headers.Add("Cookie", authCookie);
        using var cookieMeResp = await client.SendAsync(cookieMeReq);
        Assert.Equal(HttpStatusCode.OK, cookieMeResp.StatusCode);

        using var logoutReq = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("/api/auth/cookie-logout", UriKind.Relative)
        );
        logoutReq.Headers.Add("Cookie", authCookie);
        using var logoutResp = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);
    }

    private static async Task VerifyCoreDefinedTypeAndCategoryApisAsync(
        HttpClient client,
        string adminToken
    )
    {
        using var createDtReq = CreateBearerPost(
            "/api/core/defined-types",
            adminToken,
            new
            {
                name = "Shirt Sizes",
                description = "Apparel sizes",
                order = 0,
            }
        );
        using var createDtResp = await client.SendAsync(createDtReq);
        Assert.Equal(HttpStatusCode.Created, createDtResp.StatusCode);
        using var dtDoc = JsonDocument.Parse(await createDtResp.Content.ReadAsStringAsync());
        var dtId = dtDoc.RootElement.GetProperty("id").GetGuid();

        using var addValReq = CreateBearerPost(
            $"/api/core/defined-types/{dtId:D}/values",
            adminToken,
            new
            {
                definedTypeId = dtId,
                value = "Large",
                description = "Adult Large",
                attributes = new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["sku"] = "LG",
                },
            }
        );
        using var addValResp = await client.SendAsync(addValReq);
        Assert.Equal(HttpStatusCode.Created, addValResp.StatusCode);

        await AssertJsonArrayGetAsync(
            client,
            $"/api/core/defined-types/{dtId:D}/values/by-attribute?key=sku&value=LG",
            adminToken,
            minCount: 1
        );
        await AssertJsonArrayGetAsync(
            client,
            "/api/core/categories/tree?entityTypeKey=Ruth.Core.DefinedType",
            adminToken,
            minCount: 0
        );
    }

    private static async Task AssertJsonArrayGetAsync(
        HttpClient client,
        string url,
        string accessToken,
        int minCount
    )
    {
        using var req = CreateBearerGet(url, accessToken);
        using var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() >= minCount);
    }

    private static HttpRequestMessage CreateBearerGet(string url, string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri(url, UriKind.Relative));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return req;
    }

    private static HttpRequestMessage CreateBearerPost(string url, string accessToken, object body)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(url, UriKind.Relative))
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return req;
    }
}
