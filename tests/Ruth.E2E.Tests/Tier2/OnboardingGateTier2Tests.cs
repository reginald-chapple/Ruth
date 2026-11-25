using System.Globalization;
using System.Net;
using System.Text;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class OnboardingGateTier2Tests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task OnboardingGate_UnonboardedPostAndPutToRandomRoutes_AreRejectedNever200()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();

            using var postBody = new StringContent("{}", Encoding.UTF8, "application/json");
            using var postResp = await client.PostAsync(
                new Uri("/api/unexempt-gate-check", UriKind.Relative),
                postBody
            );

            using var putBody = new StringContent("{}", Encoding.UTF8, "application/json");
            using var putResp = await client.PutAsync(
                new Uri("/api/unexempt-gate-check", UriKind.Relative),
                putBody
            );

            await AssertRejectedByOnboardingGateAsync(postResp);
            await AssertRejectedByOnboardingGateAsync(putResp);
        });
    }

    [Fact]
    public async Task OnboardingGate_PostOnboardingGetOnboarding_RedirectsToRoot()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, _) =>
        {
            using var response = await client.GetAsync(new Uri("/onboarding", UriKind.Relative));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            Assert.True(
                string.Equals(location, "/", StringComparison.Ordinal) || location.EndsWith('/'),
                $"Expected redirect to '/', got '{location}'."
            );
        });
    }

    [Fact]
    public async Task OnboardingGate_SecondOnboardingPost_Returns409Or403ProblemDetails()
    {
        await Tier2TestHelpers.RunOnboardedAsync(_containers, async (_, client, _, probe) =>
        {
            var replay = TestOnboardingPayloads.CreateValidPayload(
                organizationName: "Second Org",
                adminUserName: "secondadmin",
                adminEmail: "second@gracechurch.test"
            );
            using var response = await Tier2TestHelpers.PostOnboardingAsync(client, replay);

            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.Conflict,
                HttpStatusCode.Forbidden
            );
            Assert.Equal(1, await probe.CountUserAccountsAsync());
            Assert.Equal(1, await probe.CountOrganizationProfilesAsync());
        });
    }

    [Fact]
    public async Task Onboarding_PrimaryBranchShortCodeNullAndWhitespace_HandledCleanly()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var nullShortCode = TestOnboardingPayloads.CreateValidPayload(
                primaryBranchShortCode: null
            );
            await Tier2TestHelpers.CompleteOnboardingAsync(client, nullShortCode);
        });

        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var whitespaceCode = TestOnboardingPayloads.CreateValidPayload(
                primaryBranchShortCode: "   "
            );
            using var resp = await Tier2TestHelpers.PostOnboardingAsync(client, whitespaceCode);
            Assert.True(
                resp.StatusCode
                    is HttpStatusCode.OK
                        or HttpStatusCode.Created
                        or HttpStatusCode.BadRequest,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Unexpected status {(int)resp.StatusCode} for whitespace short code."
                )
            );
        });
    }

    [Fact]
    public async Task Onboarding_UnicodeAndAdversarialEncoding_PersistsWithExactFidelity()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var payload = TestOnboardingPayloads.CreateAdversarialEncodingPayload();
            await Tier2TestHelpers.CompleteOnboardingAsync(client, payload);

            var orgRow = Assert.Single(await probe.ReadOrganizationProfilesAsync());
            Assert.True(DatabaseProbe.TryGetColumnValue(orgRow, "Name", out var storedName));
            Assert.Equal("O'Reilly & Sons <Church> \"Hope\" 🚀", storedName?.ToString());
        });
    }

    [Fact]
    public async Task Onboarding_OrganizationNameLengthBoundary_AcceptsValidLongRejects2048()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            var longValid = TestOnboardingPayloads.CreateValidPayload(
                organizationName: new string('B', 120)
            );
            await Tier2TestHelpers.CompleteOnboardingAsync(client, longValid);
        });

        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var resp = await Tier2TestHelpers.PostOnboardingAsync(
                client,
                TestOnboardingPayloads.CreateOversizedOrganizationPayload()
            );
            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(resp, HttpStatusCode.BadRequest);
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);
        });
    }

    private static async Task AssertRejectedByOnboardingGateAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
            response,
            HttpStatusCode.ServiceUnavailable
        );
    }
}
