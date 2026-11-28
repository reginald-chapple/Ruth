using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class W8S1OutboundSsrfAndWebhookIntegrationTests(HostContainersFixture containers)
    : IDisposable
{
    private const string AdminPassword = "TaxExemptStrong#2026!";

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task OutboundSsrfGuards_BlockMetadataLoopbackAndDockerTargets_AcrossWorkflowIdentityAndCms()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (_, host) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Harbor Relief Foundation",
            "Harbor Relief",
            "harbor-relief",
            "52-8811223",
            "sec@harborrelief.org",
            AdminPassword
        );
        var (token, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            host,
            AdminPassword
        );

        await VerifyIdentityOidcSsrfRejectionAsync(client, host, token);
        await VerifyCmsMediaSyncSsrfRejectionAsync(client, host, token);
        await VerifyWorkflowWebhookSsrfRejectionAsync(client, host, token);
    }

    [Fact]
    public async Task InboundWebhooks_RejectUnsignedStaleAndReplayedPayloads_AcrossAllFourProviders()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CrossTenantCanaryTestHelper.CreateMultiTenantSaasFactory(
            containers,
            isolatedDb
        );
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        var (_, host) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Summit Community Trust",
            "Summit Trust",
            "summit-trust",
            "52-9922334",
            "sec@summittrust.org",
            AdminPassword
        );
        var (token, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            host,
            AdminPassword
        );

        await VerifyStripeConnectWebhookSecurityAsync(client, host, token);
        await VerifyCommunicationWebhookSecurityAsync(client, host);
        await VerifyCheckrAndBackgroundCheckWebhookSecurityAsync(client, host, token);
        await VerifyPaymentGatewayWebhookSecurityAsync(client, host, token);
    }

    private static async Task VerifyIdentityOidcSsrfRejectionAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        string[] blockedAuthorities =
        [
            "http://169.254.169.254/latest/meta-data",
            "http://127.0.0.1:8080",
            "http://redis:6379",
        ];

        foreach (var blockedAuthority in blockedAuthorities)
        {
            using var upsert = CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Put,
                host,
                "/api/auth/oidc/providers",
                new
                {
                    providerKey = "ssrf-idp",
                    displayName = "SSRF IdP",
                    authority = blockedAuthority,
                    clientId = "cid",
                    clientSecret = "sec",
                    scopes = "openid profile email",
                    isEnabled = true,
                    autoLinkByEmail = false,
                    callbackPath = "/signin-oidc",
                },
                bearerToken: token
            );
            using var upsertResp = await client.SendAsync(upsert);
            Assert.Equal(HttpStatusCode.OK, upsertResp.StatusCode);

            using var start = CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                host,
                "/api/auth/oidc/ssrf-idp/start"
            );
            using var startResp = await client.SendAsync(start);
            Assert.Equal(HttpStatusCode.BadRequest, startResp.StatusCode);
        }
    }

    private static async Task VerifyCmsMediaSyncSsrfRejectionAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        using var createMedia = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/cms/media/accounts",
            new
            {
                name = "SSRF Media Probe",
                slug = "ssrf-media-probe",
                providerKey = "hls",
                accountIdentifier = "probe-1",
                settings = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["FeedUrl"] =
                        "http://169.254.169.254/latest/meta-data/iam/security-credentials/",
                },
            },
            bearerToken: token
        );
        using var createdResp = await client.SendAsync(createMedia);
        Assert.Equal(HttpStatusCode.Created, createdResp.StatusCode);
        using var createdDoc = JsonDocument.Parse(await createdResp.Content.ReadAsStringAsync());
        var mediaAccountId = createdDoc.RootElement.GetProperty("id").GetGuid().ToString("D");

        using var syncReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            $"/api/cms/media/accounts/{mediaAccountId}/sync",
            new { },
            bearerToken: token
        );
        using var syncResp = await client.SendAsync(syncReq);
        Assert.Equal(HttpStatusCode.BadRequest, syncResp.StatusCode);

        using var elementsReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Get,
            host,
            $"/api/cms/media/elements?mediaAccountId={mediaAccountId}",
            bearerToken: token
        );
        using var elementsResp = await client.SendAsync(elementsReq);
        using var elementsDoc = JsonDocument.Parse(await elementsResp.Content.ReadAsStringAsync());
        Assert.Equal(0, elementsDoc.RootElement.GetArrayLength());
    }

    private static async Task VerifyWorkflowWebhookSsrfRejectionAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        using var createWfType = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/workflows/types",
            BuildSsrfWorkflowTypePayload(),
            bearerToken: token
        );
        using var wfTypeResp = await client.SendAsync(createWfType);
        Assert.Equal(HttpStatusCode.Created, wfTypeResp.StatusCode);
        using var wfTypeDoc = JsonDocument.Parse(await wfTypeResp.Content.ReadAsStringAsync());
        var wfTypeId = wfTypeDoc.RootElement.GetProperty("id").GetGuid();

        using var launchReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/workflows/instances/launch",
            new
            {
                workflowTypeIdOrSlug = wfTypeId.ToString("D"),
                name = "Attempt SSRF",
                processImmediately = true,
            },
            bearerToken: token
        );
        using var launchResp = await client.SendAsync(launchReq);
        Assert.Equal(HttpStatusCode.Created, launchResp.StatusCode);
        var rawBody = await launchResp.Content.ReadAsStringAsync();
        using var instDoc = JsonDocument.Parse(rawBody);
        Assert.Equal("Faulted", instDoc.RootElement.GetProperty("status").GetString());
        Assert.Contains("169.254.169.254", rawBody, StringComparison.Ordinal);
    }

    private static object BuildSsrfWorkflowTypePayload()
    {
        return new
        {
            name = "SSRF Webhook Guard Test",
            slug = "ssrf-webhook-guard",
            workTerm = "Item",
            workflowIdPrefix = "WF-",
            isActive = true,
            maxProcessingAttempts = 1,
            activities = new[]
            {
                new
                {
                    key = "post_hook",
                    name = "Post Hook",
                    isActivatedWithWorkflow = true,
                    order = 0,
                    actions = new[]
                    {
                        new
                        {
                            key = "hook_1",
                            name = "Metadata SSRF Attempt",
                            actionPluginKey = "workflow.integrations.webhook-post",
                            order = 0,
                            configuration = new Dictionary<string, string>(StringComparer.Ordinal)
                            {
                                ["Url"] = "http://169.254.169.254/latest/meta-data/",
                            },
                        },
                    },
                },
            },
        };
    }

    private static async Task VerifyStripeConnectWebhookSecurityAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        var accountId = await StartStripeOnboardingAsync(client, host, token);
        var payload = BuildStripeAccountUpdatedPayload(accountId);
        const string endpoint = "/api/v1/tenancy/webhooks/stripe-connect";
        var secret = TenancyOptions.DefaultDevelopmentStripeConnectWebhookSecret;

        await AssertWebhookLifecycleAsync(
            client,
            host,
            endpoint,
            payload,
            "Stripe-Signature",
            secret
        );
    }

    private static async Task<string> StartStripeOnboardingAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        using var startReq = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/v1/tenancy/stripe-connect/onboarding",
            new
            {
                returnUrl = "/admin/tenancy/compliance",
                refreshUrl = "/admin/tenancy/compliance",
            },
            bearerToken: token
        );
        using var startResp = await client.SendAsync(startReq);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        using var startDoc = JsonDocument.Parse(await startResp.Content.ReadAsStringAsync());
        return startDoc.RootElement.GetProperty("accountId").GetString()!;
    }

    private static string BuildStripeAccountUpdatedPayload(string accountId)
    {
        return JsonSerializer.Serialize(
            new
            {
                id = "evt_sc_w8s1_1",
                type = "account.updated",
                data = new
                {
                    @object = new
                    {
                        id = accountId,
                        charges_enabled = true,
                        payouts_enabled = true,
                        details_submitted = true,
                    },
                },
            }
        );
    }

    private static async Task VerifyCommunicationWebhookSecurityAsync(
        HttpClient client,
        string host
    )
    {
        const string secret = "ruth-dev-comms-webhook-hmac-secret-change-in-production";
        var eventsPayload = JsonSerializer.Serialize(
            new
            {
                events = new[]
                {
                    new { eventType = "Opened", destinationAddress = "donor@example.org" },
                },
            }
        );

        await AssertWebhookLifecycleAsync(
            client,
            host,
            "/api/communication/webhooks/events",
            eventsPayload,
            "X-Ruth-Webhook-Signature",
            secret
        );
    }

    private static async Task VerifyCheckrAndBackgroundCheckWebhookSecurityAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        var requestId = await CreateBackgroundCheckRequestAsync(client, host, token);
        var payload =
            $$"""{"requestId":"{{requestId}}","status":"clear","responseId":"rep_w8s1"}""";

        await AssertWebhookLifecycleAsync(
            client,
            host,
            "/api/crm/webhooks/background-checks/in-memory",
            payload,
            "X-Ruth-Signature",
            InMemoryBackgroundCheckProvider.DefaultWebhookSecret
        );
    }

    private static async Task<string> CreateBackgroundCheckRequestAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        using var createPerson = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/crm/people",
            new
            {
                firstName = "Jordan",
                lastName = "Volunteer",
                email = "jordan.vol@example.org",
            },
            bearerToken: token
        );
        using var personResp = await client.SendAsync(createPerson);
        Assert.Equal(HttpStatusCode.Created, personResp.StatusCode);
        using var personDoc = JsonDocument.Parse(await personResp.Content.ReadAsStringAsync());
        var personId = personDoc.RootElement.GetProperty("id").GetGuid();

        using var reqCheck = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/crm/background-checks/request",
            new
            {
                personId,
                providerKey = "in-memory",
                packageName = "standard-volunteer",
            },
            bearerToken: token
        );
        using var checkResp = await client.SendAsync(reqCheck);
        Assert.Equal(HttpStatusCode.Created, checkResp.StatusCode);
        using var checkDoc = JsonDocument.Parse(await checkResp.Content.ReadAsStringAsync());
        return checkDoc.RootElement.GetProperty("requestId").GetString()!;
    }

    private static async Task VerifyPaymentGatewayWebhookSecurityAsync(
        HttpClient client,
        string host,
        string token
    )
    {
        using var createGateway = CrossTenantCanaryTestHelper.CreateHostRequest(
            HttpMethod.Post,
            host,
            "/api/finance/gateways",
            new
            {
                name = "Test Gateway W8S1",
                providerKey = "test",
                isTestMode = true,
                supportsCards = true,
                supportsAch = true,
            },
            bearerToken: token
        );
        using var gwResp = await client.SendAsync(createGateway);
        Assert.Equal(HttpStatusCode.Created, gwResp.StatusCode);
        using var gwDoc = JsonDocument.Parse(await gwResp.Content.ReadAsStringAsync());
        var gatewayId = gwDoc.RootElement.GetProperty("id").GetGuid().ToString("D");

        var body = JsonSerializer.Serialize(
            new
            {
                eventId = "evt_gw_w8s1",
                eventKind = "SubscriptionChargeSucceeded",
                transactionId = "tx_1",
                amount = 25.00m,
                currencyCode = "USD",
            }
        );
        var endpoint = $"/api/finance/gateways/{gatewayId}/webhooks";

        using var unsignedResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            body,
            headerName: null,
            headerValue: null
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unsignedResp.StatusCode);

        var staleSig = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            body,
            "ruth-dev-test-gateway-webhook-secret",
            DateTimeOffset.UtcNow.AddMinutes(-15)
        );
        using var staleResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            body,
            "X-Ruth-Signature",
            staleSig
        );
        Assert.Equal(HttpStatusCode.Unauthorized, staleResp.StatusCode);
    }

    private static async Task AssertWebhookLifecycleAsync(
        HttpClient client,
        string host,
        string endpoint,
        string payload,
        string signatureHeaderName,
        string secret
    )
    {
        using var unsignedResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            payload,
            headerName: null,
            headerValue: null
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unsignedResp.StatusCode);

        var staleSig = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            payload,
            secret,
            DateTimeOffset.UtcNow.AddMinutes(-12)
        );
        using var staleResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            payload,
            signatureHeaderName,
            staleSig
        );
        Assert.Equal(HttpStatusCode.Unauthorized, staleResp.StatusCode);

        var freshSig = WebhookHmacReplayVerifier.CreateTimestampedSignatureHeader(
            payload,
            secret,
            DateTimeOffset.UtcNow
        );
        using var freshResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            payload,
            signatureHeaderName,
            freshSig
        );
        Assert.Equal(HttpStatusCode.OK, freshResp.StatusCode);

        using var replayResp = await SendRawWebhookAsync(
            client,
            host,
            endpoint,
            payload,
            signatureHeaderName,
            freshSig
        );
        Assert.Equal(HttpStatusCode.Unauthorized, replayResp.StatusCode);
    }

    private static Task<HttpResponseMessage> SendRawWebhookAsync(
        HttpClient client,
        string host,
        string path,
        string jsonBody,
        string? headerName,
        string? headerValue
    )
    {
        var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"http://{host}{path}"))
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
        };
        req.Headers.Host = host;
        if (!string.IsNullOrWhiteSpace(headerName) && headerValue is not null)
        {
            req.Headers.Add(headerName, headerValue);
        }

        return client.SendAsync(req);
    }
}
