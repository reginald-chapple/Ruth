using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class HostPipelineAndOpenApiTier2Tests(E2EContainersFixture containers)
{
    private static readonly string[] _forbiddenOpenApiSecrets =
    [
        "passwordHash",
        "signingKey",
        "secretKey",
        "connectionString",
    ];

    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task Pipeline_ProductionErrorResponses_HideStackTracesAndInternalDetails()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, client, _, _) =>
            {
                using var badReq = await Tier2TestHelpers.PostOnboardingJsonAsync(client, "{bad");
                _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
                    badReq,
                    HttpStatusCode.BadRequest
                );

                using var notFoundReq = await client.GetAsync(
                    new Uri("/api/does-not-exist-pipeline-test", UriKind.Relative)
                );
                var notFoundBody = await notFoundReq.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.NotFound, notFoundReq.StatusCode);
                Tier2TestHelpers.AssertNoStackTraceLeak(notFoundBody);
            },
            environment: "Production"
        );
    }

    [Fact]
    public async Task Pipeline_UnsupportedHttpMethodOnOnboardingApi_Returns405MethodNotAllowed()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();

            using var response = await client.DeleteAsync(
                new Uri("/api/onboarding", UriKind.Relative)
            );
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        });
    }

    [Fact]
    public async Task Pipeline_UnsupportedContentType_Returns415UnsupportedMediaType()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var content = new StringContent("plain-text-body", Encoding.UTF8, "text/plain");
            using var response = await client.PostAsync(
                new Uri("/api/onboarding", UriKind.Relative),
                content
            );

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        });
    }

    [Fact]
    public async Task Pipeline_UnacceptableAcceptHeader_HandledWithoutServerError()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri("/health/live", UriKind.Relative)
            );
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/x-nonexistent-format")
            );

            using var response = await client.SendAsync(request);
            Assert.True((int)response.StatusCode < 500, "Must not return 5xx for Accept header.");
        });
    }

    [Fact]
    public async Task Pipeline_OversizedOnboardingBody_RejectedCleanlyWithoutServerCrash()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var hugeValue = new string('Z', 1_200_000);
            var hugeJson = $"{{\"organizationName\":\"{hugeValue}\"}}";

            using var response = await Tier2TestHelpers.PostOnboardingJsonAsync(client, hugeJson);
            Assert.True(
                response.StatusCode
                    is HttpStatusCode.BadRequest
                        or HttpStatusCode.RequestEntityTooLarge
                        or HttpStatusCode.UnprocessableEntity,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Unexpected status {(int)response.StatusCode} for oversized payload."
                )
            );
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);

            using var live = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
            Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        });
    }

    [Fact]
    public async Task OpenApi_InProductionEnvironment_IsNotExposedPublicly()
    {
        await Tier2TestHelpers.RunOnboardedAsync(
            _containers,
            async (_, client, _, _) =>
            {
                using var response = await client.GetAsync(
                    new Uri("/openapi/v1.json", UriKind.Relative)
                );
                Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
                Assert.True(
                    response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden
                );
            },
            environment: "Production"
        );
    }

    [Fact]
    public async Task OpenApi_InDevelopment_DoesNotLeakSecretFieldsInSchemas()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(
                new Uri("/openapi/v1.json", UriKind.Relative)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();

            foreach (var secretField in _forbiddenOpenApiSecrets)
            {
                Assert.DoesNotContain(secretField, json, StringComparison.OrdinalIgnoreCase);
            }
        });
    }

    [Fact]
    public async Task OpenApi_UnknownDocumentVersion_Returns404()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(
                new Uri("/openapi/v99.json", UriKind.Relative)
            );
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        });
    }

    [Fact]
    public async Task OpenApi_PostMethodOnDocumentEndpoint_Returns405Or404()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var empty = new StringContent(string.Empty);
            using var response = await client.PostAsync(
                new Uri("/openapi/v1.json", UriKind.Relative),
                empty
            );
            Assert.True(
                response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound
            );
        });
    }

    [Fact]
    public async Task OpenApi_Document_IsValidOpenApi3JsonWithoutSwashbuckle()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(
                new Uri("/openapi/v1.json", UriKind.Relative)
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("Swashbuckle", json, StringComparison.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("openapi", out var version));
            Assert.StartsWith("3.", version.GetString() ?? string.Empty, StringComparison.Ordinal);
            Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
            Assert.Equal(JsonValueKind.Object, paths.ValueKind);
        });
    }

    [Fact]
    public async Task Pipeline_RateLimitingExceeded_Returns429ProblemDetails_WhileHealthStaysExempt()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["RateLimiting:PermitLimit"] = "2",
            ["RateLimiting:WindowSeconds"] = "60",
        };
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(
                _containers,
                conn,
                overrides: overrides
            );
            using var client = factory.CreateNoRedirectClient();

            for (var i = 0; i < 2; i++)
            {
                using var allowed = await Tier2HttpHelpers.GetRelativeAsync(
                    client,
                    "/api/onboarding/status"
                );
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
            }

            using var limited = await Tier2HttpHelpers.GetRelativeAsync(
                client,
                "/api/onboarding/status"
            );
            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
                limited,
                HttpStatusCode.TooManyRequests
            );

            await Tier2HttpHelpers.AssertHealthLiveOkAsync(client);
        });
    }
}
