using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ruth.Host.IntegrationTests.Fixtures;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class Wave4FinanceEndpointsIntegrationTests(HostContainersFixture containers)
{
    private const string AdminPassword = "Wave4FinanceStrongPassword#2026!";
    private const string TestWebhookSecret = "wave4-host-webhook-secret";

    [Fact]
    public async Task FinanceEndpoints_Auth_Ledger_Scanner_Webhooks_OpenApi_AndBlazorPages_WorkEndToEnd()
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
                ["Finance:Security:TestGatewayWebhookSecret"] = TestWebhookSecret,
            }
        );
        using var client = factory.CreateNoRedirectClient();

        var token = await OnboardAndLoginAsync(client);
        var (accountId, orgPersonId) = await VerifyAccountsOrganizationsAndPledgesAsync(
            client,
            token
        );
        var (gatewayId, methodId) = await VerifyGatewaysAndSavedPaymentMethodsAsync(
            client,
            token,
            orgPersonId
        );
        await VerifyInvoicesBatchesTransactionsAndScannerAsync(
            client,
            token,
            accountId,
            orgPersonId
        );
        await VerifyRecurringWebhooksCadenceAndOpenApiAsync(
            client,
            token,
            accountId,
            gatewayId,
            methodId,
            orgPersonId
        );
        await VerifyFinanceBlazorPagesAsync(client);
    }

    private static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Finance Test Organization",
                timeZoneId = "America/Denver",
                primaryBranchName = "Central Branch",
                primaryBranchShortCode = "CENT",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var unauth = await client.GetAsync(
            new Uri("/api/finance/accounts", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static async Task<(
        Guid AccountId,
        Guid OrgPersonId
    )> VerifyAccountsOrganizationsAndPledgesAsync(HttpClient client, string token)
    {
        using var accResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/accounts",
            new
            {
                name = "Community Scholarship Fund",
                publicName = "Scholarship Fund",
                code = "SCHOLAR-W4",
                accountKind = "CharitableFund",
                isTaxDeductible = true,
                defaultCurrencyCode = "USD",
            }
        );
        Assert.Equal(HttpStatusCode.Created, accResp.StatusCode);
        Assert.NotNull(accResp.Headers.Location);
        using var accDoc = JsonDocument.Parse(await accResp.Content.ReadAsStringAsync());
        var accountId = accDoc.RootElement.GetProperty("id").GetGuid();

        using var orgResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/organizations",
            new
            {
                name = "Summit Civic Partners",
                email = "giving@summit.test",
                sponsorTier = "Gold",
            }
        );
        Assert.Equal(HttpStatusCode.Created, orgResp.StatusCode);
        using var orgDoc = JsonDocument.Parse(await orgResp.Content.ReadAsStringAsync());
        var orgPersonId = orgDoc.RootElement.GetProperty("organizationPersonId").GetGuid();

        using var pledgeResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/pledges",
            new
            {
                personId = orgPersonId,
                accountId,
                totalAmount = 2400.00m,
                currencyCode = "USD",
                frequency = "Monthly",
                startDate = "2026-01-01",
                endDate = "2026-12-31",
            }
        );
        Assert.Equal(HttpStatusCode.Created, pledgeResp.StatusCode);
        return (accountId, orgPersonId);
    }

    private static async Task<(
        Guid GatewayId,
        Guid MethodId
    )> VerifyGatewaysAndSavedPaymentMethodsAsync(HttpClient client, string token, Guid personId)
    {
        using var gwReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/gateways?activeOnly=true"
        );
        gwReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var gwResp = await client.SendAsync(gwReq);
        Assert.Equal(HttpStatusCode.OK, gwResp.StatusCode);
        using var gwDoc = JsonDocument.Parse(await gwResp.Content.ReadAsStringAsync());
        var gatewayId = gwDoc.RootElement[0].GetProperty("id").GetGuid();

        using var vaultResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/payment-methods",
            new
            {
                personId,
                financialGatewayId = gatewayId,
                hostedPaymentToken = "tok_host_visa_4242",
                methodKind = "CreditCard",
                maskedLast4Hint = "4242",
                expiryMonthHint = 12,
                expiryYearHint = 2030,
            }
        );
        Assert.Equal(HttpStatusCode.Created, vaultResp.StatusCode);
        using var vaultDoc = JsonDocument.Parse(await vaultResp.Content.ReadAsStringAsync());
        return (gatewayId, vaultDoc.RootElement.GetProperty("id").GetGuid());
    }

    private static async Task VerifyInvoicesBatchesTransactionsAndScannerAsync(
        HttpClient client,
        string token,
        Guid accountId,
        Guid personId
    )
    {
        using var invResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/invoices",
            new
            {
                personId,
                issueDate = "2026-04-01",
                dueDate = "2026-05-01",
                currencyCode = "USD",
                lineItems = new[]
                {
                    new
                    {
                        accountId,
                        description = "Annual Sponsorship",
                        quantity = 1m,
                        unitAmount = 500.00m,
                    },
                },
                issueImmediately = true,
            }
        );
        Assert.Equal(HttpStatusCode.Created, invResp.StatusCode);
        using var invDoc = JsonDocument.Parse(await invResp.Content.ReadAsStringAsync());
        var invoiceId = invDoc.RootElement.GetProperty("id").GetGuid();

        using var txResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/transactions",
            new
            {
                authorizedPersonId = personId,
                currencyCode = "USD",
                sourceType = "StaffEntry",
                tenderType = "Wire",
                splits = new[]
                {
                    new
                    {
                        accountId,
                        amount = 500.00m,
                        invoiceId,
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Created, txResp.StatusCode);

        using var scanResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/check-scanner/ingest",
            new
            {
                routingNumber = "021000021",
                accountNumber = "1122334455",
                checkNumber = "2040",
                amount = 175.00m,
                currencyCode = "USD",
                accountId,
                overridePersonId = personId,
                frontImageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("FAKE-PNG-CHECK")),
            }
        );
        Assert.Equal(HttpStatusCode.Created, scanResp.StatusCode);
    }

    private static async Task VerifyRecurringWebhooksCadenceAndOpenApiAsync(
        HttpClient client,
        string token,
        Guid accountId,
        Guid gatewayId,
        Guid methodId,
        Guid personId
    )
    {
        var todayText = DateOnly
            .FromDateTime(DateTimeOffset.UtcNow.UtcDateTime)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        using var recResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/recurring",
            new
            {
                personId,
                financialGatewayId = gatewayId,
                savedPaymentMethodId = methodId,
                purpose = "CharitableContribution",
                frequency = "Monthly",
                currencyCode = "USD",
                startDate = todayText,
                splits = new[] { new { accountId, amount = 100.00m } },
            }
        );
        Assert.Equal(HttpStatusCode.Created, recResp.StatusCode);
        using var recDoc = JsonDocument.Parse(await recResp.Content.ReadAsStringAsync());
        var subRef = recDoc.RootElement.GetProperty("gatewaySubscriptionReference").GetString();

        var webhookBody =
            $$"""{"eventId":"evt_host_1","eventKind":"SubscriptionChargeSucceeded","subscriptionReference":"{{subRef}}","transactionId":"tx_host_1","amount":100.00,"currencyCode":"USD"}""";
        using var badWhResp = await SendSignedWebhookAsync(
            client,
            gatewayId,
            webhookBody,
            "sha256=deadbeef"
        );
        Assert.Equal(HttpStatusCode.Unauthorized, badWhResp.StatusCode);

        var validMac = Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(TestWebhookSecret),
                Encoding.UTF8.GetBytes(webhookBody)
            )
        );
        using var goodWhResp = await SendSignedWebhookAsync(
            client,
            gatewayId,
            webhookBody,
            $"sha256={validMac}"
        );
        Assert.Equal(HttpStatusCode.OK, goodWhResp.StatusCode);

        using var scanResp = await SendAuthJsonAsync(
            client,
            token,
            HttpMethod.Post,
            "/api/finance/cadence-alerts/scan",
            new { }
        );
        Assert.Equal(HttpStatusCode.OK, scanResp.StatusCode);

        using var openApiResp = await client.GetAsync(
            new Uri("/openapi/v1.json", UriKind.Relative)
        );
        Assert.Equal(HttpStatusCode.OK, openApiResp.StatusCode);
        var openApiText = await openApiResp.Content.ReadAsStringAsync();
        Assert.Contains("/api/finance/accounts", openApiText, StringComparison.Ordinal);
        Assert.Contains("/api/finance/check-scanner/ingest", openApiText, StringComparison.Ordinal);
    }

    private static async Task VerifyFinanceBlazorPagesAsync(HttpClient client)
    {
        foreach (
            var route in new[]
            {
                "/finance/accounts",
                "/finance/organizations",
                "/admin/finance/gateways",
                "/finance/invoices",
                "/finance/batches",
                "/finance/recurring",
            }
        )
        {
            using var response = await client.GetAsync(new Uri(route, UriKind.Relative));
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain("FluentTextField", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "An unhandled error has occurred",
                html,
                StringComparison.OrdinalIgnoreCase
            );
        }
    }

    private static Task<HttpResponseMessage> SendAuthJsonAsync(
        HttpClient client,
        string token,
        HttpMethod method,
        string relativeUrl,
        object payload
    )
    {
        var request = new HttpRequestMessage(method, relativeUrl)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendSignedWebhookAsync(
        HttpClient client,
        Guid gatewayId,
        string body,
        string signature
    )
    {
        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/finance/gateways/{gatewayId:D}/webhooks"
        )
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.Add("X-Ruth-Signature", signature);
        return client.SendAsync(req);
    }
}
