using System.Net;
using System.Text.Json.Nodes;
using Ruth.E2E.Tests.Fixtures;
using Xunit;

namespace Ruth.E2E.Tests.Tier2;

[Collection(E2ETestCollection.Name)]
public sealed class SecurityAndIdentityTier2Tests(E2EContainersFixture containers)
{
    private readonly E2EContainersFixture _containers = containers;

    [Fact]
    public async Task ProductionGuard_MissingSigningKey_FailsFastWithNonZeroExit()
    {
        await AssertProductionRejectsSigningKeyAsync(signingKey: null);
    }

    [Fact]
    public async Task ProductionGuard_EmptyAndWhitespaceSigningKey_FailsFastWithNonZeroExit()
    {
        await AssertProductionRejectsSigningKeyAsync(string.Empty);
        await AssertProductionRejectsSigningKeyAsync(new string(' ', 32));
    }

    [Fact]
    public async Task ProductionGuard_ThirtyOneCharSigningKey_FailsFastAtBvaBoundary()
    {
        await AssertProductionRejectsSigningKeyAsync(new string('A', 31));
    }

    [Fact]
    public async Task ProductionGuard_ExactDevSentinelSigningKey_FailsFastWithNonZeroExit()
    {
        await AssertProductionRejectsSigningKeyAsync(RuthHostFactory.DefaultDevJwtSigningKey);
    }

    [Fact]
    public async Task ProductionGuard_ThirtyTwoCharValidSigningKey_StartsSuccessfully()
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, _) =>
        {
            var valid32 = "AbCdEfGhIjKlMnOpQrStUvWxYz123456";
            var env = Tier2TestHelpers.BuildProcessEnvironment(
                _containers,
                conn,
                "Production",
                valid32,
                bootstrapAutoMigrate: "true"
            );

            await using var host = await HostProcessRunner.StartHostAsync(
                env,
                TimeSpan.FromSeconds(25)
            );
            using var resp = await host.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        });
    }

    [Fact]
    public async Task IdentityOnboarding_InvalidTimeZoneId_Returns400WithZeroDbRows()
    {
        await AssertInvalidOnboardingPayloadAsync(
            TestOnboardingPayloads.CreateInvalidTimeZonePayload()
        );
    }

    [Fact]
    public async Task IdentityOnboarding_WeakPassword_Returns400WithZeroDbRows()
    {
        await AssertInvalidOnboardingPayloadAsync(
            TestOnboardingPayloads.CreateWeakPasswordPayload()
        );
    }

    [Fact]
    public async Task IdentityOnboarding_InvalidEmail_Returns400WithZeroDbRows()
    {
        await AssertInvalidOnboardingPayloadAsync(
            TestOnboardingPayloads.CreateInvalidEmailPayload()
        );
    }

    [Fact]
    public async Task IdentityOnboarding_EmptyOrganizationName_Returns400WithZeroDbRows()
    {
        await AssertInvalidOnboardingPayloadAsync(
            TestOnboardingPayloads.CreateMissingOrganizationNamePayload()
        );
    }

    [Fact]
    public async Task IdentityOnboarding_MissingAdminUserName_Returns400WithZeroDbRows()
    {
        await AssertInvalidOnboardingPayloadAsync(
            TestOnboardingPayloads.CreateMissingAdminUserNamePayload()
        );
    }

    private async Task AssertProductionRejectsSigningKeyAsync(string? signingKey)
    {
        var env = Tier2TestHelpers.BuildProcessEnvironment(
            _containers,
            _containers.PostgresAdminConnectionString,
            "Production",
            signingKey,
            bootstrapAutoMigrate: "false"
        );

        var result = await HostProcessRunner.RunHostAsync(env, TimeSpan.FromSeconds(15));
        Assert.False(result.TimedOut, "Production guard must fail fast without hanging.");
        Assert.NotEqual(0, result.ExitCode);
    }

    private async Task AssertInvalidOnboardingPayloadAsync(JsonObject payload)
    {
        await Tier2TestHelpers.RunWithIsolatedDatabaseAsync(_containers, async (_, conn, probe) =>
        {
            await using var factory = RuthHostFactory.Create(_containers, conn);
            using var client = factory.CreateNoRedirectClient();
            using var response = await Tier2TestHelpers.PostOnboardingAsync(client, payload);

            _ = await Tier2TestHelpers.AssertProblemDetailsAsync(
                response,
                HttpStatusCode.BadRequest
            );
            await Tier2TestHelpers.AssertZeroIdentityRowsAsync(probe);
        });
    }
}
