using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Identity.Infrastructure;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class OnboardingControlCharacterRegressionTests(HostContainersFixture containers)
{
    public static TheoryData<string, Func<object>> NulBytePayloads =>
        new()
        {
            { "OrganizationName", () => CreatePayload(organizationName: "Org\0Name") },
            { "PrimaryCampusName", () => CreatePayload(primaryCampusName: "Main\0Campus") },
            { "PrimaryCampusShortCode", () => CreatePayload(primaryCampusShortCode: "SC\01") },
            { "AdminUserName", () => CreatePayload(adminUserName: "admin\0evil") },
            { "AdminDisplayName", () => CreatePayload(adminDisplayName: "Admin\0Name") },
            { "AdminEmail", () => CreatePayload(adminEmail: "admin\0@grace.org") },
            { "TimeZoneId", () => CreatePayload(timeZoneId: "UTC\0") },
        };

    [Fact]
    public async Task PostOnboarding_NulByteInAnyTextField_Returns400AndInsertsZeroRows()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
        };

        await using var factory = HostWebApplicationFactory.Create(
            isolatedDb,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            environment: "Development",
            overrides: overrides
        );
        using var client = factory.CreateNoRedirectClient();

        foreach (var row in NulBytePayloads)
        {
            var payload = ((Func<object>)row[1])();
            using var response = await client.PostAsJsonAsync(
                new Uri("/api/onboarding", UriKind.Relative),
                payload
            );
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain("22021", body, StringComparison.Ordinal);
            Assert.DoesNotContain("DbUpdateException", body, StringComparison.Ordinal);
            Assert.DoesNotContain("PostgresException", body, StringComparison.Ordinal);
        }

        await AssertDatabaseEmptyAsync(factory);

        using var validResponse = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            CreatePayload()
        );
        Assert.Equal(HttpStatusCode.Created, validResponse.StatusCode);
    }

    private static async Task AssertDatabaseEmptyAsync(HostWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        Assert.Equal(0, await db.OrganizationProfiles.CountAsync());
        Assert.Equal(0, await db.UserAccounts.CountAsync());
        Assert.Equal(0, await db.SecurityRoles.CountAsync());
        Assert.Equal(0, await db.UserRoles.CountAsync());
        Assert.Equal(0, await db.OutboxMessages.CountAsync());
    }

    private static object CreatePayload(
        string organizationName = "Grace Fellowship",
        string timeZoneId = "UTC",
        string primaryCampusName = "Main Campus",
        string? primaryCampusShortCode = "MAIN",
        string adminUserName = "admin",
        string adminEmail = "admin@grace.org",
        string adminDisplayName = "Grace Admin"
    )
    {
        return new
        {
            organizationName,
            timeZoneId,
            primaryCampusName,
            primaryCampusShortCode,
            adminUserName,
            adminEmail,
            adminDisplayName,
            adminPassword = "StrongOnboarding#2026!",
        };
    }
}
