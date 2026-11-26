using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave3CommunicationEndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave3CommsStrongPassword#2026!";

    [Fact]
    public async Task CommunicationEndpoints_Transports_PhoneNumbers_Devices_Send_OpenApi_AndBlazorPages_WorkEndToEnd()
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
        await VerifyTransportsAndDevicesEndpointsAsync(client, token);
        await VerifySendAndLogsEndpointsAsync(client, token);
        await VerifyBulkCampaignsWebhooksAndTwoWaySmsAsync(client, token);
        await VerifyOpenApiAndBlazorPagesAsync(client);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Comms Test Org",
                timeZoneId = "America/Denver",
                primaryBranchName = "Main Branch",
                primaryBranchShortCode = "MAIN",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var unauth = await client.GetAsync(
            new Uri("/api/communication/transports", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var doc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task VerifyTransportsAndDevicesEndpointsAsync(
        HttpClient client,
        string token
    )
    {
        using var trReq = new HttpRequestMessage(HttpMethod.Get, "/api/communication/transports");
        trReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var trResp = await client.SendAsync(trReq);
        Assert.Equal(HttpStatusCode.OK, trResp.StatusCode);
        using var trDoc = JsonDocument.Parse(await trResp.Content.ReadAsStringAsync());
        Assert.Equal(7, trDoc.RootElement.GetArrayLength());

        using var phoneReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/communication/phone-numbers"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    name = "Community Helpline SMS",
                    number = "+15553334444",
                    isSmsEnabled = true,
                }
            ),
        };
        phoneReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var phoneResp = await client.SendAsync(phoneReq);
        Assert.Equal(HttpStatusCode.Created, phoneResp.StatusCode);

        using var devReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/communication/personal-devices"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    platform = "Android",
                    pushRegistrationToken = "fcm-host-test-token-1234567890",
                    deviceName = "Pixel 9",
                    notificationsEnabled = true,
                }
            ),
        };
        devReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var devResp = await client.SendAsync(devReq);
        Assert.Equal(HttpStatusCode.OK, devResp.StatusCode);
    }

    private static async Task VerifySendAndLogsEndpointsAsync(HttpClient client, string token)
    {
        using var sendReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/communication/system-communications/send"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    systemCommunicationKey = "finance.payment-or-gift-receipt",
                    recipientEmail = "supporter@example.org",
                    recipientName = "Riley Chen",
                    recipientPhoneNumber = "+15558889999",
                    mergeFields = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["ReceiptNumber"] = "HOST-100",
                        ["Amount"] = "$75.00",
                    },
                }
            ),
        };
        sendReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var sendResp = await client.SendAsync(sendReq);
        Assert.Equal(HttpStatusCode.OK, sendResp.StatusCode);

        using var logsReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/communication/system-communications/logs?limit=10"
        );
        logsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var logsResp = await client.SendAsync(logsReq);
        Assert.Equal(HttpStatusCode.OK, logsResp.StatusCode);
    }

    private static async Task VerifyBulkCampaignsWebhooksAndTwoWaySmsAsync(
        HttpClient client,
        string token
    )
    {
        using var createCamp = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/communication/campaigns"
        )
        {
            Content = JsonContent.Create(
                new
                {
                    title = "Host Bulk Digest",
                    channel = "Email",
                    topicKey = "community-digest",
                    subject = "Host Digest — {{ OrganizationName }}",
                    sections = new[]
                    {
                        new
                        {
                            order = 0,
                            kind = "RichText",
                            heading = "Welcome",
                            bodyMarkup = "<p>Hello {{ RecipientName }}</p>",
                        },
                    },
                    recipients = new[]
                    {
                        new
                        {
                            destinationAddress = "hostrecipient@example.org",
                            displayName = "Host Member",
                        },
                    },
                }
            ),
        };
        createCamp.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var campResp = await client.SendAsync(createCamp);
        Assert.Equal(HttpStatusCode.Created, campResp.StatusCode);
        using var campDoc = JsonDocument.Parse(await campResp.Content.ReadAsStringAsync());
        var campaignId = campDoc.RootElement.GetProperty("id").GetGuid();

        using var approveReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/communication/campaigns/{campaignId:D}/approve"
        )
        {
            Content = JsonContent.Create(new { communicationId = campaignId }),
        };
        approveReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var approveResp = await client.SendAsync(approveReq);
        Assert.Equal(HttpStatusCode.OK, approveResp.StatusCode);

        using var dispatchReq = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/communication/campaigns/dispatch-due"
        )
        {
            Content = JsonContent.Create(new { specificCommunicationId = campaignId }),
        };
        dispatchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var dispatchResp = await client.SendAsync(dispatchReq);
        Assert.Equal(HttpStatusCode.OK, dispatchResp.StatusCode);

        using var webhookResp = await client.PostAsJsonAsync(
            new Uri("/api/communication/webhooks/events", UriKind.Relative),
            new
            {
                events = new[]
                {
                    new { eventType = "Opened", destinationAddress = "hostrecipient@example.org" },
                },
            }
        );
        Assert.Equal(HttpStatusCode.OK, webhookResp.StatusCode);

        using var smsWebhookResp = await client.PostAsJsonAsync(
            new Uri("/api/communication/sms/webhook", UriKind.Relative),
            new
            {
                fromPhoneNumber = "+15557771234",
                toPhoneNumber = "+15553334444",
                messageBody = "HOURS",
            }
        );
        Assert.Equal(HttpStatusCode.OK, smsWebhookResp.StatusCode);
    }

    private static async Task VerifyOpenApiAndBlazorPagesAsync(HttpClient client)
    {
        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/communication/transports", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/communication/campaigns", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/communication/flows", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/communication/sms/webhook", openApiText, StringComparison.Ordinal);
        Assert.Contains(
            "/api/communication/system-communications/send",
            openApiText,
            StringComparison.Ordinal
        );

        foreach (
            var route in new[]
            {
                "/admin/communication/templates",
                "/admin/communication/transports",
                "/communication/campaigns",
                "/communication/flows",
                "/communication/sms-inbox",
                "/communication/preferences",
            }
        )
        {
            using var pageResp = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await pageResp.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, pageResp.StatusCode);
            Assert.DoesNotContain("FluentTextField", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fluent-text-field", html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
