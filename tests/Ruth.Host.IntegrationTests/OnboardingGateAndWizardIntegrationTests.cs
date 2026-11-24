using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Host.IntegrationTests.Fixtures;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Xunit;

namespace Ruth.Host.IntegrationTests;

[Collection(HostIntegrationFixtures.Name)]
public sealed class OnboardingGateAndWizardIntegrationTests(HostContainersFixture containers)
{
    private static readonly string[] _forbiddenV4Markers =
    [
        "FluentTextField",
        "fluent-text-field",
        "FluentDesignTheme",
        "FluentNavMenu",
        "Appearance.Accent",
    ];

    [Fact]
    public async Task ZeroSeed_BeforeOnboarding_GatesRoutesAndExemptsHealthOpenApiAndOnboarding()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateLiveOnboardingFactory(isolatedDb);
        using var client = factory.CreateNoRedirectClient();

        await AssertRedirectsToOnboardingAsync(client, "/");
        await AssertRedirectsToOnboardingAsync(client, "/staff/dashboard");

        using var apiBlocked = await client.GetAsync(new Uri("/api/nonexistent", UriKind.Relative));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, apiBlocked.StatusCode);
        Assert.Contains(
            "application/problem+json",
            apiBlocked.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );

        using var emptyPost = new StringContent(string.Empty);
        using var postBlocked = await client.PostAsync(new Uri("/", UriKind.Relative), emptyPost);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, postBlocked.StatusCode);

        using var live = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        using var ready = await client.GetAsync(new Uri("/health", UriKind.Relative));
        using var openApi = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);

        var statusResp = await client.GetFromJsonAsync<OnboardingStatusResponse>(
            new Uri("/api/onboarding/status", UriKind.Relative)
        );
        Assert.NotNull(statusResp);
        Assert.True(statusResp.IsOnboardingRequired);
        Assert.False(statusResp.IsSelfRegistrationLocked);
        Assert.Null(statusResp.OrganizationName);

        using var wizardResp = await client.GetAsync(new Uri("/onboarding", UriKind.Relative));
        var html = await wizardResp.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, wizardResp.StatusCode);
        Assert.Contains("Welcome to Ruth RMS", html, StringComparison.Ordinal);
        Assert.Contains("fluent-text-input", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-option", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("org-timezone", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fluent-button", html, StringComparison.OrdinalIgnoreCase);

        foreach (var marker in _forbiddenV4Markers)
        {
            Assert.DoesNotContain(marker, html, StringComparison.OrdinalIgnoreCase);
        }

        using var cssResp = await client.GetAsync(new Uri("/app.css", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, cssResp.StatusCode);
    }

    [Fact]
    public async Task PostOnboarding_InvalidPayload_Returns400AndLeavesDatabaseEmpty()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateLiveOnboardingFactory(isolatedDb);
        using var client = factory.CreateNoRedirectClient();

        var invalidPayload = new
        {
            organizationName = "",
            timeZoneId = "Invalid/TimeZone",
            primaryCampusName = "Main Campus",
            primaryCampusShortCode = "MAIN",
            adminUserName = "admin",
            adminEmail = "not-an-email",
            adminDisplayName = "Admin User",
            adminPassword = "weak",
        };

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            invalidPayload
        );
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType,
            StringComparison.OrdinalIgnoreCase
        );
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(0, await db.OrganizationProfiles.CountAsync());
        Assert.Equal(0, await db.UserAccounts.CountAsync());

        await AssertRedirectsToOnboardingAsync(client, "/");
    }

    [Fact]
    public async Task PostOnboarding_ValidPayload_PersistsIdentityAndPermanentlyLocksAcrossRestarts()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        const string password = "StrongOnboarding#2026!";

        await using (var factory = CreateLiveOnboardingFactory(isolatedDb))
        {
            using var client = factory.CreateNoRedirectClient();

            using var createResp = await client.PostAsJsonAsync(
                new Uri("/api/onboarding", UriKind.Relative),
                CreateValidPayload(password)
            );
            var createBody = await createResp.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            Assert.DoesNotContain(password, createBody, StringComparison.Ordinal);
            Assert.DoesNotContain("passwordHash", createBody, StringComparison.OrdinalIgnoreCase);

            var statusAfter = await client.GetFromJsonAsync<OnboardingStatusResponse>(
                new Uri("/api/onboarding/status", UriKind.Relative)
            );
            Assert.NotNull(statusAfter);
            Assert.False(statusAfter.IsOnboardingRequired);
            Assert.True(statusAfter.IsSelfRegistrationLocked);
            Assert.Equal("Grace Fellowship", statusAfter.OrganizationName);

            await AssertRedirectsToHomeAsync(client, "/onboarding");

            using var emptyPost = new StringContent(string.Empty);
            using var lockedPostPage = await client.PostAsync(
                new Uri("/onboarding", UriKind.Relative),
                emptyPost
            );
            Assert.Equal(HttpStatusCode.Conflict, lockedPostPage.StatusCode);

            using var homeResp = await client.GetAsync(new Uri("/", UriKind.Relative));
            var homeHtml = await homeResp.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, homeResp.StatusCode);
            Assert.Contains("<h1>Ruth RMS</h1>", homeHtml, StringComparison.Ordinal);
            Assert.Contains("fluent-nav", homeHtml, StringComparison.OrdinalIgnoreCase);

            using var duplicateResp = await client.PostAsJsonAsync(
                new Uri("/api/onboarding", UriKind.Relative),
                CreateValidPayload(password, "admin2", "admin2@grace.org")
            );
            Assert.Equal(HttpStatusCode.Conflict, duplicateResp.StatusCode);
            Assert.Contains(
                "application/problem+json",
                duplicateResp.Content.Headers.ContentType?.MediaType,
                StringComparison.OrdinalIgnoreCase
            );

            await AssertDatabaseSeededCorrectlyAsync(factory, password);
        }

        // Simulate host restart against the exact same PostgreSQL database
        await using var restartedFactory = CreateLiveOnboardingFactory(isolatedDb);
        using var restartedClient = restartedFactory.CreateNoRedirectClient();

        using var restartedHome = await restartedClient.GetAsync(new Uri("/", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, restartedHome.StatusCode);

        await AssertRedirectsToHomeAsync(restartedClient, "/onboarding");

        using var restartedDuplicate = await restartedClient.PostAsJsonAsync(
            new Uri("/api/onboarding", UriKind.Relative),
            CreateValidPayload(password, "admin3", "admin3@grace.org")
        );
        Assert.Equal(HttpStatusCode.Conflict, restartedDuplicate.StatusCode);
    }

    [Fact]
    public async Task PostOnboarding_ConcurrentRace_AllowsStrictlyOneWinner()
    {
        var isolatedDb = await containers.CreateIsolatedDatabaseAsync();
        await using var factory = CreateLiveOnboardingFactory(isolatedDb);

        var clients = Enumerable
            .Range(0, 5)
            .Select(_ => factory.CreateNoRedirectClient())
            .ToArray();
        try
        {
            var responses = await Task.WhenAll(
                clients.Select(
                    async (client, index) =>
                    {
                        var user = string.Create(CultureInfo.InvariantCulture, $"admin{index}");
                        var email = string.Create(
                            CultureInfo.InvariantCulture,
                            $"admin{index}@grace.org"
                        );
                        return await client.PostAsJsonAsync(
                            new Uri("/api/onboarding", UriKind.Relative),
                            CreateValidPayload("RacePass#2026!", user, email)
                        );
                    }
                )
            );

            _ = Assert.Single(responses, static r => r.StatusCode == HttpStatusCode.Created);
            Assert.Equal(4, responses.Count(static r => r.StatusCode == HttpStatusCode.Conflict));

            foreach (var r in responses)
            {
                r.Dispose();
            }
        }
        finally
        {
            foreach (var c in clients)
            {
                c.Dispose();
            }
        }

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.Equal(1, await db.OrganizationProfiles.CountAsync());
        Assert.Equal(1, await db.UserAccounts.CountAsync());
    }

    private HostWebApplicationFactory CreateLiveOnboardingFactory(string isolatedDbConnection)
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

    private static object CreateValidPayload(
        string password,
        string userName = "admin",
        string email = "admin@grace.org"
    )
    {
        return new
        {
            organizationName = "Grace Fellowship",
            timeZoneId = "UTC",
            primaryCampusName = "Main Campus",
            primaryCampusShortCode = "MAIN",
            adminUserName = userName,
            adminEmail = email,
            adminDisplayName = "Grace Admin",
            adminPassword = password,
        };
    }

    private static async Task AssertRedirectsToOnboardingAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/onboarding", response.Headers.Location?.OriginalString);
    }

    private static async Task AssertRedirectsToHomeAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
    }

    private static async Task AssertDatabaseSeededCorrectlyAsync(
        HostWebApplicationFactory factory,
        string plaintextPassword
    )
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var org = await db.OrganizationProfiles.SingleAsync();
        var role = await db.SecurityRoles.SingleAsync();
        var user = await db.UserAccounts.Include(static u => u.Roles).SingleAsync();
        var outbox = await db.OutboxMessages.SingleAsync();

        Assert.Equal("Grace Fellowship", org.Name);
        Assert.True(org.IsOnboardingCompleted);
        Assert.True(org.SelfRegistrationLocked);
        Assert.Equal(SecurityRole.AdministratorsRoleId, role.Id);
        Assert.Equal("admin", user.UserName);
        Assert.NotEqual(plaintextPassword, user.PasswordHash, StringComparer.Ordinal);
        _ = Assert.Single(user.Roles);
        Assert.Contains(
            nameof(OrganizationOnboardedIntegrationEvent),
            outbox.EventType,
            StringComparison.Ordinal
        );

        using var payloadDoc = JsonDocument.Parse(outbox.Payload);
        Assert.Equal(
            "Grace Fellowship",
            payloadDoc.RootElement.GetProperty("organizationName").GetString()
        );
    }
}
