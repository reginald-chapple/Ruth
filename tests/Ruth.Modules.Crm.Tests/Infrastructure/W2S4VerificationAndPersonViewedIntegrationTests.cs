using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W2S4VerificationAndPersonViewedIntegrationTests(
    CrmPostgresContainerFixture fixture
)
{
    [Fact]
    public async Task IdentityVerification_RateLimit_AndPersonViewedAudit_WorkEndToEnd()
    {
        var conn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await VerifyIdentityChallengeAndRateLimitAsync(commands);
        await VerifyPersonViewedAuditAsync(commands, queries);
    }

    private static async Task VerifyIdentityChallengeAndRateLimitAsync(ICommandDispatcher commands)
    {
        var issued = (
            await commands.SendAsync<
                IssueIdentityVerificationCommand,
                IdentityVerificationIssuedResponse
            >(new IssueIdentityVerificationCommand("+16025550100", "203.0.113.5", DailyIpLimit: 1))
        ).Value;

        var rateLimited = await commands.SendAsync<
            IssueIdentityVerificationCommand,
            IdentityVerificationIssuedResponse
        >(new IssueIdentityVerificationCommand("+16025550100", "203.0.113.5", DailyIpLimit: 1));
        Assert.Equal(CrmErrors.IdentityVerificationIpLimitReached, rateLimited.PrimaryError);

        var verified = (
            await commands.SendAsync<VerifyIdentityCodeCommand, IdentityVerificationResultResponse>(
                new VerifyIdentityCodeCommand(
                    issued.VerificationId,
                    issued.DispatchedCodeForTransport
                )
            )
        ).Value;
        Assert.True(verified.IsVerified);
    }

    private static async Task VerifyPersonViewedAuditAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var personId = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Noah", "Walker")
            )
        )
            .Value
            .Id;

        _ = await commands.SendAsync<RecordPersonViewedCommand, PersonViewedResponse>(
            new RecordPersonViewedCommand(personId, Source: "IntegrationTest")
        );

        var viewedHistory = (
            await queries.QueryAsync<
                GetPersonViewedHistoryQuery,
                IReadOnlyList<PersonViewedResponse>
            >(new GetPersonViewedHistoryQuery(personId))
        ).Value;
        _ = Assert.Single(viewedHistory);

        var reloaded = (
            await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(personId)
            )
        ).Value;
        Assert.Equal(1, reloaded.ViewedCount);
    }
}
