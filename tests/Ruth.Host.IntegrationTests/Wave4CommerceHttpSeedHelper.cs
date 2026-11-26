using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ruth.Host.IntegrationTests;

internal static class Wave4CommerceHttpSeedHelper
{
    private const string AdminPassword = "Wave4CommerceStrongPassword#2026!";

    public static async Task<string> OnboardAndLoginAsync(HttpClient client)
    {
        using var onboard = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            new
            {
                organizationName = "Ruth Commerce S9-S10 Org",
                timeZoneId = "America/Denver",
                primaryBranchName = "Downtown Center",
                primaryBranchShortCode = "DTWN",
                adminUserName = "admin",
                adminEmail = "admin@ruth.test",
                adminDisplayName = "System Admin",
                adminPassword = AdminPassword,
            }
        );
        Assert.Equal(HttpStatusCode.Created, onboard.StatusCode);

        using var login = await client.PostAsJsonAsync(
            new Uri("/api/auth/login", UriKind.Relative),
            new { userNameOrEmail = "admin", password = AdminPassword }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    public static async Task<(
        Guid OperatingAccId,
        Guid CharitableAccId,
        Guid GatewayId
    )> LoadFinanceSeedsAsync(HttpClient client, string token)
    {
        using var nonDedReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/accounts?activeOnly=true&isTaxDeductible=false"
        );
        nonDedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var nonDedResp = await client.SendAsync(nonDedReq);
        using var nonDedDoc = JsonDocument.Parse(await nonDedResp.Content.ReadAsStringAsync());
        var opId = nonDedDoc.RootElement[0].GetProperty("id").GetGuid();

        using var dedReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/accounts?activeOnly=true&isTaxDeductible=true"
        );
        dedReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var dedResp = await client.SendAsync(dedReq);
        using var dedDoc = JsonDocument.Parse(await dedResp.Content.ReadAsStringAsync());
        var charId = dedDoc.RootElement[0].GetProperty("id").GetGuid();

        using var gwReq = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/finance/gateways?activeOnly=true"
        );
        gwReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var gwResp = await client.SendAsync(gwReq);
        using var gwDoc = JsonDocument.Parse(await gwResp.Content.ReadAsStringAsync());
        var gwId = gwDoc.RootElement[0].GetProperty("id").GetGuid();

        return (opId, charId, gwId);
    }

    public static Task<HttpResponseMessage> SendAuthJsonAsync(
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
}
