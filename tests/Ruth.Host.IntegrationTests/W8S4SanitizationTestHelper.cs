using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Providers.CommunicationTransports.Mailgun;
using Ruth.Providers.CommunicationTransports.SendGrid;
using Ruth.Providers.CommunicationTransports.Smtp;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class W8S4SanitizationTestHelper
{
    public static async Task VerifyCmsHtmlBlockSanitizesXssEndToEndAsync(
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        var pageId = await ProvisionSimpleCmsPageAsync(client, hostA, tokenA);

        using var blockResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/cms/blocks",
                new
                {
                    blockTypeKey = "cms.html-content",
                    zone = "Main",
                    name = "Announcement",
                    pageId,
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, blockResp.StatusCode);
        using var blockDoc = JsonDocument.Parse(await blockResp.Content.ReadAsStringAsync());
        var blockId = blockDoc.RootElement.GetProperty("id").GetGuid();

        using var htmlResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                $"/api/cms/blocks/{blockId:D}/html",
                new
                {
                    blockId,
                    content = "<script>alert(1)</script><svg onload=\"alert(2)\"></svg><p style=\"color:#111;width:expression(alert(3))\">Welcome <a href=\"javascript:alert(4)\">bad</a> <a href=\"https://orchardaid.org/donate\" target=\"_blank\">Donate</a></p>",
                    autoApprove = true,
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.Created, htmlResp.StatusCode);

        using var resolveResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Get,
                hostA,
                "/api/cms/public/resolve?siteSlug=orchard-public"
            )
        );
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        var resolved = await resolveResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script", resolved, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", resolved, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", resolved, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expression(", resolved, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Welcome", resolved, StringComparison.Ordinal);
        Assert.Contains("noopener noreferrer", resolved, StringComparison.Ordinal);
    }

    public static async Task VerifyTemplatePreviewAndTransportsBlockXssAndCrlfAsync(
        IServiceProvider services,
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        var contextJson = JsonSerializer.Serialize(
            new
            {
                Name = "<b>Ada</b>\r\nBcc: spy@evil.test",
                ActionUrl = "javascript:alert(document.cookie)",
            }
        );
        using var previewResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/communication/templates/preview-merge",
                new
                {
                    template = "<p>Hi {{Name}}, <a href=\"{{ActionUrl}}\">Open Receipt</a></p><script>evil()</script>",
                    contextJson,
                    outputMode = "Html",
                },
                bearerToken: tokenA
            )
        );
        Assert.Equal(HttpStatusCode.OK, previewResp.StatusCode);
        using var previewDoc = JsonDocument.Parse(await previewResp.Content.ReadAsStringAsync());
        var html = previewDoc.RootElement.GetProperty("renderedContent").GetString()!;
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;b&gt;Ada&lt;/b&gt;", html, StringComparison.Ordinal);

        await W8S4IntegrationTestHelper.WriteSanitizedEmailArtifactForHtmlHintAsync(html);
        await VerifyCrlfHeaderRejectionAcrossAllEmailTransportsAsync(
            services,
            client,
            hostA,
            tokenA
        );
    }

    private static async Task<Guid> ProvisionSimpleCmsPageAsync(
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        string[] domains = [hostA];
        string[] zones = ["Main"];
        string[] rootRoutes = [""];

        using var siteResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/cms/sites",
                new
                {
                    name = "Orchard Public",
                    slug = "orchard-public",
                    domains,
                },
                bearerToken: tokenA
            )
        );
        using var siteDoc = JsonDocument.Parse(await siteResp.Content.ReadAsStringAsync());
        var siteId = siteDoc.RootElement.GetProperty("id").GetGuid();

        using var layoutResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/cms/layouts",
                new
                {
                    siteId,
                    name = "Standard",
                    slug = "standard",
                    zones,
                },
                bearerToken: tokenA
            )
        );
        using var layoutDoc = JsonDocument.Parse(await layoutResp.Content.ReadAsStringAsync());
        var layoutId = layoutDoc.RootElement.GetProperty("id").GetGuid();

        using var pageResp = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/cms/pages",
                new
                {
                    siteId,
                    layoutId,
                    internalName = "Landing",
                    pageTitle = "Orchard Landing",
                    setAsSiteDefault = true,
                    routes = rootRoutes,
                },
                bearerToken: tokenA
            )
        );
        using var pageDoc = JsonDocument.Parse(await pageResp.Content.ReadAsStringAsync());
        return pageDoc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task VerifyCrlfHeaderRejectionAcrossAllEmailTransportsAsync(
        IServiceProvider services,
        HttpClient client,
        string hostA,
        string tokenA
    )
    {
        using var crlfSend = await client.SendAsync(
            CrossTenantCanaryTestHelper.CreateHostRequest(
                HttpMethod.Post,
                hostA,
                "/api/communication/system-communications/send",
                new
                {
                    systemCommunicationKey = "finance.payment-or-gift-receipt",
                    recipientEmail = "donor@example.org\r\nBcc: attacker@evil.test",
                    recipientName = "Riley",
                    mergeFields = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["ReceiptNumber"] = "R-900",
                        ["Amount"] = "$50.00",
                    },
                },
                bearerToken: tokenA
            )
        );
        Assert.False(crlfSend.IsSuccessStatusCode);

        var clock = services.GetRequiredService<IClock>();
        var httpFactory = services.GetRequiredService<IHttpClientFactory>();
        var poisonedMsg = new OutboundTransportMessage(
            Guid.CreateVersion7(),
            CommunicationChannel.Email,
            "victim@example.org\r\nRCPT TO:<spy@evil.test>",
            "Victim",
            "donor@harborrelief.org",
            "Harbor",
            ReplyToAddress: null,
            "Subject\r\nBcc: spy@evil.test",
            "<p>Body</p>",
            "Body"
        );

        var smtp = new SmtpCommunicationTransport(
            Options.Create(new SmtpTransportOptions { Host = "127.0.0.1", Port = 2525 }),
            clock
        );
        Assert.True((await smtp.SendAsync(poisonedMsg)).IsFailure);

        var sg = new SendGridCommunicationTransport(
            httpFactory,
            Options.Create(new SendGridTransportOptions { ApiKey = "SG.test" }),
            clock
        );
        Assert.True((await sg.SendAsync(poisonedMsg)).IsFailure);

        var mg = new MailgunCommunicationTransport(
            httpFactory,
            Options.Create(new MailgunTransportOptions { ApiKey = "key-test", Domain = "mg.test" }),
            clock
        );
        Assert.True((await mg.SendAsync(poisonedMsg)).IsFailure);
    }
}
