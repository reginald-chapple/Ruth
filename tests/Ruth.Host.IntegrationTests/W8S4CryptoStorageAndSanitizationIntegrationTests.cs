using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class W8S4CryptoStorageAndSanitizationIntegrationTests(
    HostContainersFixture containers
) : IDisposable
{
    private const string AdminPassword = "TaxExemptW8S4#2026!";

    public void Dispose()
    {
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task CredentialsAtRest_AreTenantEnvelopeEncryptedInPostgres_RedactedInApi_AndRejectedCrossTenant()
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
            "Harbor Relief Foundation",
            "Harbor Relief",
            "harbor-relief",
            "52-7711221",
            "sec@harborrelief.org",
            AdminPassword
        );
        var (tenantBId, _) = await CrossTenantCanaryTestHelper.ProvisionTaxExemptTenantAsync(
            client,
            "Summit Relief Foundation",
            "Summit Relief",
            "summit-relief",
            "52-7711222",
            "sec@summitrelief.org",
            AdminPassword
        );
        var (tokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            AdminPassword
        );

        await ConfigureOidcAndGatewayCredentialsAsync(client, hostA, tokenA);
        await ConfigureCommsAndMediaCredentialsAsync(client, hostA, tokenA);
        await W8S4IntegrationTestHelper.VerifyRawPostgresCiphertextAndCrossTenantRejectionAsync(
            factory.Services,
            isolatedDb,
            tenantAId,
            tenantBId
        );
    }

    [Fact]
    public async Task RustFsUploads_RejectSvgPolyglotAndPathTraversal_ClampPresignedExpiry_AndServeAttachmentHeaders()
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
            "Beacon Aid Society",
            "Beacon Aid",
            "beacon-aid",
            "52-7711223",
            "sec@beaconaid.org",
            AdminPassword
        );
        var (tokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            AdminPassword
        );

        using var typeResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/core/binary-file-types",
                new
                {
                    name = "Grant Attachments",
                    storageProviderKey = "rustfs",
                    allowedExtensions = ".png,.pdf",
                    maxFileSizeBytes = 2_097_152,
                    requiresViewSecurity = true,
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, typeResp.StatusCode);
        using var typeDoc = JsonDocument.Parse(await typeResp.Content.ReadAsStringAsync());
        var typeId = typeDoc.RootElement.GetProperty("id").GetGuid();

        await W8S4IntegrationTestHelper.AssertMaliciousUploadsRejectedAsync(
            client,
            hostA,
            tokenA,
            typeId
        );
        await AssertValidUploadPresignClampAndDownloadHeadersAsync(
            client,
            hostA,
            tokenA,
            typeId,
            tenantAId
        );
    }

    [Fact]
    public async Task CmsHtml_MergeTemplates_AndEmailTransports_SanitizeOwaspXssAndRejectCrlfInjection()
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
            "Orchard Aid Collective",
            "Orchard Aid",
            "orchard-aid",
            "52-7711224",
            "sec@orchardaid.org",
            AdminPassword
        );
        var (tokenA, _) = await CrossTenantCanaryTestHelper.LoginTenantAdminAsync(
            client,
            hostA,
            AdminPassword
        );

        await W8S4SanitizationTestHelper.VerifyCmsHtmlBlockSanitizesXssEndToEndAsync(
            client,
            hostA,
            tokenA
        );
        await W8S4SanitizationTestHelper.VerifyTemplatePreviewAndTransportsBlockXssAndCrlfAsync(
            factory.Services,
            client,
            hostA,
            tokenA
        );
    }

    private static async Task ConfigureOidcAndGatewayCredentialsAsync(
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        using var oidcResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Put,
                hostA,
                "/api/auth/oidc/providers",
                new
                {
                    providerKey = "okta-sso",
                    displayName = "Okta Workforce",
                    authority = "https://idp.harborrelief.org",
                    clientId = "harbor-client",
                    clientSecret = "oidc_super_secret_harbor_token_9911",
                    scopes = "openid profile email",
                    isEnabled = true,
                    autoLinkByEmail = false,
                    callbackPath = "/signin-oidc-okta",
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, oidcResp.StatusCode);

        using var gwCreate = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/finance/gateways",
                new
                {
                    name = "Harbor Stripe Live",
                    providerKey = "test-gateway",
                    isActive = true,
                    isTestMode = true,
                    supportsCards = true,
                    supportsAch = true,
                    feeCoverageOption = "Optional",
                    cardPercentFeeRate = 0.029m,
                    cardFlatFeeAmount = 0.30m,
                    achFlatFeeAmount = 0.80m,
                    settings = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["SecretKey"] = "sk_test_harbor_stripe_vault_884422",
                        ["statement_descriptor"] = "HARBOR RELIEF",
                    },
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, gwCreate.StatusCode);
        var gwBody = await gwCreate.Content.ReadAsStringAsync();
        Assert.DoesNotContain(
            "sk_test_harbor_stripe_vault_884422",
            gwBody,
            StringComparison.Ordinal
        );
        Assert.Contains("********", gwBody, StringComparison.Ordinal);
    }

    private static async Task ConfigureCommsAndMediaCredentialsAsync(
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        using var trResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Put,
                hostA,
                "/api/communication/transports/configs/sendgrid",
                new
                {
                    transportKey = "sendgrid",
                    channel = "Email",
                    isEnabled = true,
                    defaultFromAddress = "giving@harborrelief.org",
                    defaultFromName = "Harbor Giving",
                    credentials = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["ApiKey"] = "SG.harbor_sendgrid_secret_key_776655",
                    },
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, trResp.StatusCode);
        Assert.DoesNotContain(
            "SG.harbor_sendgrid_secret_key_776655",
            await trResp.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var gwList = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/finance/gateways",
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, gwList.StatusCode);
        Assert.Contains(
            "HARBOR RELIEF",
            await gwList.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var trList = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/communication/transports/configs",
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, trList.StatusCode);
        Assert.Contains(
            "********",
            await trList.Content.ReadAsStringAsync(),
            StringComparison.Ordinal
        );

        using var mediaResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/cms/media/accounts",
                new
                {
                    name = "Harbor Vimeo",
                    slug = "harbor-vimeo",
                    providerKey = "in-memory",
                    accountIdentifier = "harbor-org",
                    settings = new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["AccessToken"] = "vimeo_pat_harbor_secret_554433",
                    },
                    isActive = true,
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, mediaResp.StatusCode);
    }

    private static async Task AssertValidUploadPresignClampAndDownloadHeadersAsync(
        HttpClient client,
        string hostA,
        string tokenA,
        Guid typeId,
        Guid tenantAId
    )
    {
        using var okUpload = await W8S4IntegrationTestHelper.PostMultipartUploadAsync(
            client,
            hostA,
            tokenA,
            typeId,
            "clean-receipt.png",
            "image/png",
            W8S4IntegrationTestHelper.ValidPngBytes
        );
        Assert.Equal(HttpStatusCode.Created, okUpload.StatusCode);
        using var fileDoc = JsonDocument.Parse(await okUpload.Content.ReadAsStringAsync());
        var fileId = fileDoc.RootElement.GetProperty("id").GetGuid();
        var objectKey = fileDoc.RootElement.GetProperty("objectKey").GetString() ?? string.Empty;
        Assert.StartsWith($"tenants/{tenantAId:D}/", objectKey, StringComparison.Ordinal);

        using var presignResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/core/files/{fileId:D}/presigned-url?expiresInMinutes=1440",
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, presignResp.StatusCode);
        var presignJson = await presignResp.Content.ReadAsStringAsync();
        Assert.Contains("X-Amz-Expires=900", presignJson, StringComparison.Ordinal);
        Assert.Contains(
            "response-content-disposition=attachment",
            presignJson,
            StringComparison.Ordinal
        );

        using var dlResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                $"/api/core/files/{fileId:D}/content",
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, dlResp.StatusCode);
        Assert.Equal("attachment", dlResp.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", dlResp.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains(
            "sandbox",
            dlResp.Headers.GetValues("Content-Security-Policy").Single(),
            StringComparison.Ordinal
        );
    }
}
