using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class W8S3AuthJwtOidcAndBolaIntegrationTests(HostContainersFixture containers)
    : IDisposable
{
    private const string AdminPassword = "TaxExemptStrong#2026!";

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task JwtPinning_RejectsAlgNoneTamperedKeyConfusionHeaderInjection_AndCrossTenantTokens()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (tenantAId, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Redwood Aid Trust",
            "Redwood Aid",
            "redwood-aid",
            "52-6611223",
            "sec@redwoodaid.org",
            AdminPassword
        );
        var (_, hostB) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Cedar Aid Trust",
            "Cedar Aid",
            "cedar-aid",
            "52-6611224",
            "sec@cedaraid.org",
            AdminPassword
        );

        var (validTokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            AdminPassword
        );

        await VerifyJwtForgeryVectorsRejectedAsync(client, hostA, validTokenA);
        await VerifyCrossTenantAndOperatorPlaneJwtRejectionAsync(
            client,
            hostB,
            validTokenA,
            tenantAId
        );
    }

    [Fact]
    public async Task RefreshTokenReuse_OidcOpenRedirectAndRedirectUriPinning_AndSaltedApiKeys_EnforceStrictAuth()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (tenantId, host) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Granite Relief Society",
            "Granite Relief",
            "granite-relief",
            "52-6611225",
            "sec@graniterelief.org",
            AdminPassword
        );
        var (adminToken, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            host,
            AdminPassword
        );

        await VerifyRefreshTokenReuseRevokesFamilyAndLogsAuditAsync(client, host);
        await VerifyOidcOpenRedirectAndRedirectUriEnforcementAsync(client, host, adminToken);
        await VerifySaltedRestApiKeyLifecycleAsync(
            factory.Services,
            client,
            host,
            adminToken,
            tenantId
        );
    }

    [Fact]
    public async Task SignalRWebSocketOriginAndAuth_AndCrossConstituentCrossTenantBola_EnforceStrictIsolation()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (_, hostA) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Maple Community Fund",
            "Maple Fund",
            "maple-fund",
            "52-6611226",
            "sec@maplefund.org",
            AdminPassword
        );
        var (_, hostB) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Birch Community Fund",
            "Birch Fund",
            "birch-fund",
            "52-6611227",
            "sec@birchfund.org",
            AdminPassword
        );

        var (adminTokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            AdminPassword
        );
        var (adminTokenB, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostB,
            AdminPassword
        );

        await VerifySignalRCswshAndHubAuthorizationAsync(factory, hostA);
        await VerifyHorizontalAndCrossTenantBolaIdorRejectedAsync(
            client,
            hostA,
            hostB,
            adminTokenA,
            adminTokenB
        );
    }

    private static async Task VerifyJwtForgeryVectorsRejectedAsync(
        HttpClient client,
        string hostA,
        string validTokenA
    )
    {
        using var baselineOk = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/auth/me",
                bearerToken: validTokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, baselineOk.StatusCode);

        var parts = validTokenA.Split('.');
        var payloadSegment = parts[1];

        string[] forgedTokens =
        [
            $"{Base64UrlJson(new { alg = "none", typ = "JWT" })}.{payloadSegment}.",
            $"{Base64UrlJson(new { alg = "None", typ = "JWT" })}.{payloadSegment}.",
            $"{Base64UrlJson(new { alg = "HS384", typ = "JWT" })}.{payloadSegment}.{parts[2]}",
            $"{Base64UrlJson(new { alg = "HS256", typ = "JWT", jku = "https://evil.example/jwks.json" })}.{payloadSegment}.{parts[2]}",
            $"{Base64UrlJson(new { alg = "HS256", typ = "JWT", kid = "../../etc/passwd" })}.{payloadSegment}.{parts[2]}",
            $"{parts[0]}.{payloadSegment}.AAAA_tampered_signature_ZZZZ",
        ];

        foreach (var forged in forgedTokens)
        {
            using var resp = await client.SendAsync(
                CrossTenantCanaryTestHelper.CreateHostRequest(
                    HttpMethod.Get,
                    hostA,
                    "/api/auth/me",
                    bearerToken: forged
                )
            );
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }

    private static async Task VerifyCrossTenantAndOperatorPlaneJwtRejectionAsync(
        HttpClient client,
        string hostB,
        string validTokenA,
        Guid tenantAId
    )
    {
        // Replaying Tenant A's valid signed HS256 JWT against Tenant B MUST return 401
        using var crossTenantMe = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostB,
                "/api/auth/me",
                bearerToken: validTokenA
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, crossTenantMe.StatusCode);

        // Replaying Tenant A's valid JWT against ops.localhost MUST return 401
        using var onOpsResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                "ops.localhost",
                "/api/v1/platform/operators/queue",
                bearerToken: validTokenA
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, onOpsResp.StatusCode);
        Assert.NotEqual(Guid.Empty, tenantAId);
    }

    private static async Task VerifyRefreshTokenReuseRevokesFamilyAndLogsAuditAsync(
        HttpClient client,
        string host
    )
    {
        using var loginResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/login",
                new { userNameOrEmail = "orgadmin", password = AdminPassword }
            )
        );
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        using var loginDoc = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        var refresh1 = loginDoc.RootElement.GetProperty("refreshToken").GetString();

        // Legitimate rotation: refresh1 -> refresh2
        using var rotResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/refresh",
                new { refreshToken = refresh1 }
            )
        );
        Assert.Equal(HttpStatusCode.OK, rotResp.StatusCode);
        using var rotDoc = JsonDocument.Parse(await rotResp.Content.ReadAsStringAsync());
        var refresh2 = rotDoc.RootElement.GetProperty("refreshToken").GetString();
        var accessToken2 = rotDoc.RootElement.GetProperty("accessToken").GetString();

        // Attacker replays already-rotated refresh1 -> 401 AND revokes refresh2!
        using var replay1 = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/refresh",
                new { refreshToken = refresh1 }
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, replay1.StatusCode);

        using var afterTheft = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/refresh",
                new { refreshToken = refresh2 }
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);

        using var historyResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                host,
                "/api/auth/login-history",
                bearerToken: accessToken2
            )
        );
        var historyJson = await historyResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, historyResp.StatusCode);
        Assert.Contains("RefreshTokenReuseDetected", historyJson, StringComparison.Ordinal);
    }

    private static async Task VerifyOidcOpenRedirectAndRedirectUriEnforcementAsync(
        HttpClient client,
        string host,
        string adminToken
    )
    {
        // Wildcard or fragment OAuth client registration must fail (400)
        using var wildcardClientResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/security/clients",
                new
                {
                    name = "Evil Wildcard",
                    clientId = "evil-wild",
                    redirectUri = "https://partner.example.org/*",
                    postLogoutRedirectUri = "https://partner.example.org/logout",
                    allowedScopes = "openid",
                },
                bearerToken: adminToken
            )
        );
        Assert.Equal(HttpStatusCode.BadRequest, wildcardClientResp.StatusCode);

        string[] openRedirectPayloads =
        [
            "https://evil.attacker.example/phish",
            "//evil.attacker.example/phish",
            "/\\evil.attacker.example/phish",
            "/%2fevil.attacker.example",
            "javascript:alert(1)",
        ];
        foreach (var badReturnUrl in openRedirectPayloads)
        {
            var encoded = Uri.EscapeDataString(badReturnUrl);
            using var startResp = await client.SendAsync(
                CrossTenantCanaryTestHelper.CreateHostRequest(
                    HttpMethod.Post,
                    host,
                    $"/api/auth/oidc/entra/start?returnUrl={encoded}"
                )
            );
            Assert.Equal(HttpStatusCode.BadRequest, startResp.StatusCode);
        }

        await VerifyOidcCallbackRedirectUriMismatchAsync(client, host, adminToken);
    }

    private static async Task VerifyOidcCallbackRedirectUriMismatchAsync(
        HttpClient client,
        string host,
        string adminToken
    )
    {
        using var upsertResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Put,
                host,
                "/api/auth/oidc/providers",
                new
                {
                    providerKey = "entra-strict",
                    displayName = "Entra Strict",
                    authority = "https://login.microsoftonline.com/common/v2.0",
                    clientId = "cid-strict",
                    clientSecret = "sec-strict",
                    scopes = "openid profile email",
                    isEnabled = true,
                    autoLinkByEmail = false,
                    callbackPath = "https://granite-relief.sandbox.localhost/signin-oidc",
                },
                bearerToken: adminToken
            )
        );
        Assert.Equal(HttpStatusCode.OK, upsertResp.StatusCode);

        using var mismatchCallback = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/oidc/entra-strict/callback",
                new
                {
                    state = "any-state",
                    code = "any-code",
                    redirectUri = "https://granite-relief.sandbox.localhost/signin-oidc/extra",
                }
            )
        );
        Assert.Equal(HttpStatusCode.Unauthorized, mismatchCallback.StatusCode);
    }

    private static async Task VerifySaltedRestApiKeyLifecycleAsync(
        IServiceProvider services,
        HttpClient client,
        string host,
        string adminToken,
        Guid tenantId
    )
    {
        using var createKeyResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/security/api-keys",
                new { name = "W8S3 Integration Key", description = "Salted PBKDF2 test" },
                bearerToken: adminToken
            )
        );
        Assert.Equal(HttpStatusCode.Created, createKeyResp.StatusCode);
        using var keyDoc = JsonDocument.Parse(await createKeyResp.Content.ReadAsStringAsync());
        var plaintextKey = keyDoc.RootElement.GetProperty("plaintextApiKey").GetString()!;
        var keyId = keyDoc.RootElement.GetProperty("key").GetProperty("id").GetGuid();
        var keyPrefix = keyDoc.RootElement.GetProperty("key").GetProperty("keyPrefix").GetString()!;

        Assert.StartsWith("ruth_", plaintextKey, StringComparison.Ordinal);
        Assert.Equal(16, keyPrefix.Length);

        // Verify persisted DB hash uses per-key salted PBKDF2 format ($pbkdf2-s256$...)
        await using (var scope = services.CreateAsyncScope())
        using (AmbientTenantContext.BeginScope(TenantId.From(tenantId)))
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var stored = await db.RestApiKeys.AsNoTracking().SingleAsync();
            Assert.StartsWith("$pbkdf2-s256$100000$", stored.KeyHash, StringComparison.Ordinal);
        }

        // Authenticate via X-Api-Key
        using var apiKeyMeReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Get,
            host,
            "/api/auth/me"
        );
        apiKeyMeReq.Headers.Add("X-Api-Key", plaintextKey);
        using var apiKeyMeResp = await client.SendAsync(apiKeyMeReq);
        Assert.Equal(HttpStatusCode.OK, apiKeyMeResp.StatusCode);

        // Revoke key -> subsequent use fails with 401
        var keyIdText = keyId.ToString("D");
        using var revokeResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Delete,
                host,
                $"/api/security/api-keys/{keyIdText}",
                bearerToken: adminToken
            )
        );
        Assert.Equal(HttpStatusCode.NoContent, revokeResp.StatusCode);

        using var revokedReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Get,
            host,
            "/api/auth/me"
        );
        revokedReq.Headers.Add("X-Api-Key", plaintextKey);
        using var revokedResp = await client.SendAsync(revokedReq);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResp.StatusCode);
    }

    private static async Task VerifySignalRCswshAndHubAuthorizationAsync(
        HostWebApplicationFactory factory,
        string hostA
    )
    {
        // 1. Cross-Origin WebSocket Hijacking (CSWSH) attempt from https://evil.attacker.example -> rejected before 101 upgrade!
        var evilWsClient = factory.Server.CreateWebSocketClient();
        evilWsClient.ConfigureRequest = req =>
        {
            req.Headers.Host = hostA;
            req.Headers.Origin = "https://evil.attacker.example";
        };
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evilWsClient.ConnectAsync(
                new Uri($"ws://{hostA}/hubs/events/interactive"),
                CancellationToken.None
            )
        );

        // 2. Same-origin anonymous WebSocket connection: allowed to handshake, DENIED on [Authorize] ProjectActiveAction
        var sameOriginWsClient = factory.Server.CreateWebSocketClient();
        sameOriginWsClient.ConfigureRequest = req =>
        {
            req.Headers.Host = hostA;
            req.Headers.Origin = $"http://{hostA}";
        };
        using var anonSocket = await sameOriginWsClient.ConnectAsync(
            new Uri($"ws://{hostA}/hubs/events/interactive"),
            CancellationToken.None
        );
        var handshakeAck = await SendSignalRHandshakeAndReadAsync(anonSocket);
        Assert.Equal("{}\u001e", handshakeAck);

        var dummySessionId = Guid.CreateVersion7().ToString("D");
        var modInvoke =
            $$"""{"type":1,"invocationId":"1","target":"ProjectActiveAction","arguments":["{{dummySessionId}}",null]}"""
            + "\u001e";
        await anonSocket.SendAsync(
            Encoding.UTF8.GetBytes(modInvoke),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None
        );
        var modReply = await ReadWebSocketFrameAsync(anonSocket);
        Assert.Contains("unauthorized", modReply, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task VerifyHorizontalAndCrossTenantBolaIdorRejectedAsync(
        HttpClient client,
        string hostA,
        string hostB,
        string adminTokenA,
        string adminTokenB
    )
    {
        var (userAId, tokenUserA) = await ProvisionConfirmedConstituentAsync(
            client,
            hostA,
            adminTokenA,
            "constituent_a",
            "const.a@maplefund.org"
        );
        var (userBId, _) = await ProvisionConfirmedConstituentAsync(
            client,
            hostA,
            adminTokenA,
            "constituent_b",
            "const.b@maplefund.org"
        );
        var userAIdText = userAId.ToString("D");
        var userBIdText = userBId.ToString("D");

        // User A accessing own preferences -> 200 OK
        using var ownPref = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/core/preferences/{userAIdText}",
                bearerToken: tokenUserA
            )
        );
        Assert.Equal(HttpStatusCode.OK, ownPref.StatusCode);

        // Horizontal BOLA: User A reading or writing User B's preferences -> 403 Forbidden
        using var bolaPrefGet = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/core/preferences/{userBIdText}",
                bearerToken: tokenUserA
            )
        );
        Assert.Equal(HttpStatusCode.Forbidden, bolaPrefGet.StatusCode);

        using var bolaPrefPut = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Put,
                hostA,
                "/api/core/preferences",
                new
                {
                    ownerKey = userBId,
                    key = "ui.theme",
                    value = "hijacked",
                },
                bearerToken: tokenUserA
            )
        );
        Assert.Equal(HttpStatusCode.Forbidden, bolaPrefPut.StatusCode);

        // Horizontal BOLA: User A requesting User B's login-history -> 403 Forbidden
        using var bolaHistory = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/auth/login-history?userAccountId={userBIdText}",
                bearerToken: tokenUserA
            )
        );
        Assert.Equal(HttpStatusCode.Forbidden, bolaHistory.StatusCode);

        await VerifyCrossTenantPersonIdorReturns404Async(
            client,
            hostA,
            hostB,
            adminTokenA,
            adminTokenB
        );
    }

    private static async Task VerifyCrossTenantPersonIdorReturns404Async(
        HttpClient client,
        string hostA,
        string hostB,
        string adminTokenA,
        string adminTokenB
    )
    {
        using var createPersonB = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostB,
                "/api/crm/people",
                new
                {
                    firstName = "Private",
                    lastName = "DonorB",
                    email = "donor@birchfund.org",
                },
                bearerToken: adminTokenB
            )
        );
        Assert.Equal(HttpStatusCode.Created, createPersonB.StatusCode);
        using var personBDoc = JsonDocument.Parse(await createPersonB.Content.ReadAsStringAsync());
        var personBId = personBDoc.RootElement.GetProperty("id").GetGuid().ToString("D");

        using var crossTenantIdor = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/crm/people/{personBId}",
                bearerToken: adminTokenA
            )
        );
        Assert.Equal(HttpStatusCode.NotFound, crossTenantIdor.StatusCode);
    }

    private static async Task<(Guid UserId, string AccessToken)> ProvisionConfirmedConstituentAsync(
        HttpClient client,
        string host,
        string adminToken,
        string userName,
        string email
    )
    {
        const string password = "Constituent#2026!";
        using var created = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/users",
                new
                {
                    userName,
                    email,
                    displayName = userName,
                    password,
                    isConfirmed = true,
                },
                bearerToken: adminToken
            )
        );
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var userId = doc.RootElement.GetProperty("userAccountId").GetGuid();

        using var login = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/login",
                new { userNameOrEmail = userName, password }
            )
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return (userId, loginDoc.RootElement.GetProperty("accessToken").GetString()!);
    }

    private static async Task<string> SendSignalRHandshakeAndReadAsync(WebSocket socket)
    {
        var handshake = Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e");
        await socket.SendAsync(
            handshake,
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None
        );
        return await ReadWebSocketFrameAsync(socket);
    }

    private static async Task<string> ReadWebSocketFrameAsync(WebSocket socket)
    {
        var buffer = new byte[4096];
        var res = await socket.ReceiveAsync(buffer, CancellationToken.None);
        return res.MessageType == WebSocketMessageType.Close
            ? $"closed:{res.CloseStatusDescription}"
            : Encoding.UTF8.GetString(buffer, 0, res.Count);
    }

    private static string Base64UrlJson(object value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
