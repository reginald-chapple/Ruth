using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class W8S4IntegrationTestHelper
{
    internal static readonly byte[] ValidPngBytes =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D,
        0x49,
        0x48,
        0x44,
        0x52,
        0x00,
        0x00,
        0x00,
        0x01,
        0x00,
        0x00,
        0x00,
        0x01,
        0x08,
        0x06,
        0x00,
        0x00,
        0x00,
        0x1F,
        0x15,
        0xC4,
        0x89,
        0x00,
        0x00,
        0x00,
        0x0A,
        0x49,
        0x44,
        0x41,
        0x54,
        0x78,
        0x9C,
        0x63,
        0x00,
        0x01,
        0x00,
        0x00,
        0x05,
        0x00,
        0x01,
        0x0D,
        0x0A,
        0x2D,
        0xB4,
        0x00,
        0x00,
        0x00,
        0x00,
        0x49,
        0x45,
        0x4E,
        0x44,
        0xAE,
        0x42,
        0x60,
        0x82,
    ];

    public static async Task<string> QuerySingleStringAsync(
        string adminConnectionString,
        string sql,
        Guid tenantId
    )
    {
        await using var conn = new NpgsqlConnection(adminConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        _ = cmd.Parameters.AddWithValue("tid", tenantId);
        var raw = await cmd.ExecuteScalarAsync();
        Assert.NotNull(raw);
        return Convert.ToString(raw, CultureInfo.InvariantCulture)!;
    }

    public static async Task<HttpResponseMessage> PostMultipartUploadAsync(
        HttpClient client,
        string host,
        string bearerToken,
        Guid binaryFileTypeId,
        string fileName,
        string contentType,
        byte[] payload
    )
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(payload);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);

        var path =
            $"/api/core/files/upload?binaryFileTypeId={binaryFileTypeId:D}&isTemporary=false";
        var req = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"http://{host}{path}", UriKind.Absolute)
        )
        {
            Content = form,
        };
        req.Headers.Host = host;
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return await client.SendAsync(req);
    }

    public static void AssertEnvelopeEncryptedAndCrossTenantRejected(
        IServiceProvider services,
        Guid tenantAId,
        Guid tenantBId,
        string purpose,
        string envelopeOrJson,
        string expectedPlaintextSecret,
        bool isJsonMap,
        string? jsonKey = null
    )
    {
        Assert.DoesNotContain(expectedPlaintextSecret, envelopeOrJson, StringComparison.Ordinal);
        Assert.Contains(
            AesGcmTenantCredentialProtector.EnvelopePrefix,
            envelopeOrJson,
            StringComparison.Ordinal
        );

        var envelope = envelopeOrJson;
        if (isJsonMap && jsonKey is not null)
        {
            using var doc = JsonDocument.Parse(envelopeOrJson);
            envelope = doc.RootElement.GetProperty(jsonKey).GetString() ?? string.Empty;
        }

        var protector = services.GetRequiredService<ITenantCredentialProtector>();
        Assert.Equal(
            expectedPlaintextSecret,
            protector.Unprotect(TenantId.From(tenantAId), purpose, envelope)
        );
        _ = Assert.ThrowsAny<CryptographicException>(() =>
            protector.Unprotect(TenantId.From(tenantBId), purpose, envelope)
        );
    }

    public static async Task VerifyRawPostgresCiphertextAndCrossTenantRejectionAsync(
        IServiceProvider services,
        string adminConnectionString,
        Guid tenantAId,
        Guid tenantBId
    )
    {
        var rawOidc = await QuerySingleStringAsync(
            adminConnectionString,
            "SELECT client_secret FROM identity.external_auth_providers WHERE tenant_id = @tid AND provider_key = 'okta-sso'",
            tenantAId
        );
        AssertEnvelopeEncryptedAndCrossTenantRejected(
            services,
            tenantAId,
            tenantBId,
            "identity.external-auth.client-secret",
            rawOidc,
            "oidc_super_secret_harbor_token_9911",
            isJsonMap: false
        );

        var rawGateway = await QuerySingleStringAsync(
            adminConnectionString,
            "SELECT settings::text FROM finance.financial_gateways WHERE tenant_id = @tid AND \"Name\" = 'Harbor Stripe Live'",
            tenantAId
        );
        Assert.Contains("HARBOR RELIEF", rawGateway, StringComparison.Ordinal);
        AssertEnvelopeEncryptedAndCrossTenantRejected(
            services,
            tenantAId,
            tenantBId,
            "finance.gateway.settings:secretkey",
            rawGateway,
            "sk_test_harbor_stripe_vault_884422",
            isJsonMap: true,
            jsonKey: "SecretKey"
        );

        var rawComms = await QuerySingleStringAsync(
            adminConnectionString,
            "SELECT credentials::text FROM comms.communication_transport_configs WHERE tenant_id = @tid AND \"TransportKey\" = 'sendgrid'",
            tenantAId
        );
        AssertEnvelopeEncryptedAndCrossTenantRejected(
            services,
            tenantAId,
            tenantBId,
            "comms.transport.credentials:apikey",
            rawComms,
            "SG.harbor_sendgrid_secret_key_776655",
            isJsonMap: true,
            jsonKey: "ApiKey"
        );

        var rawMedia = await QuerySingleStringAsync(
            adminConnectionString,
            "SELECT settings::text FROM cms.media_accounts WHERE tenant_id = @tid AND \"Slug\" = 'harbor-vimeo'",
            tenantAId
        );
        AssertEnvelopeEncryptedAndCrossTenantRejected(
            services,
            tenantAId,
            tenantBId,
            "cms.media-account.settings:accesstoken",
            rawMedia,
            "vimeo_pat_harbor_secret_554433",
            isJsonMap: true,
            jsonKey: "AccessToken"
        );
    }

    public static async Task AssertMaliciousUploadsRejectedAsync(
        HttpClient client,
        string hostA,
        string tokenA,
        Guid typeId
    )
    {
        var polyglot = ValidPngBytes.Concat("<script>alert(1)</script>"u8.ToArray()).ToArray();
        using var polyResp = await PostMultipartUploadAsync(
            client,
            hostA,
            tokenA,
            typeId,
            "polyglot.png",
            "image/png",
            polyglot
        );
        Assert.Equal(HttpStatusCode.BadRequest, polyResp.StatusCode);

        using var svgResp = await PostMultipartUploadAsync(
            client,
            hostA,
            tokenA,
            typeId,
            "vector.svg",
            "image/svg+xml",
            "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray()
        );
        Assert.Equal(HttpStatusCode.BadRequest, svgResp.StatusCode);

        using var travResp = await PostMultipartUploadAsync(
            client,
            hostA,
            tokenA,
            typeId,
            "../../etc/passwd.png",
            "image/png",
            ValidPngBytes
        );
        Assert.Equal(HttpStatusCode.BadRequest, travResp.StatusCode);
    }

    public static async Task WriteSanitizedEmailArtifactForHtmlHintAsync(string innerEmailBodyHtml)
    {
        var repoRoot = FindRepoRoot();
        var outDir = Path.Combine(repoRoot, "artifacts", "htmlhint");
        _ = Directory.CreateDirectory(outDir);
        var fullDoc =
            "<!DOCTYPE html>\n"
            + "<html lang=\"en\">\n"
            + "<head><meta charset=\"utf-8\" /><title>Sanitized Receipt Preview</title></head>\n"
            + $"<body>\n{innerEmailBodyHtml}\n</body>\n"
            + "</html>\n";
        await File.WriteAllTextAsync(
            Path.Combine(outDir, "w8s4-sanitized-email.html"),
            fullDoc,
            Encoding.UTF8
        );
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ruth.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Unable to locate Ruth.slnx repository root.");
    }
}
