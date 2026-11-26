using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W5S4PortfolioAndMergeIntegrationTests(
    EngagementPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task PortfolioTouchpoints_Queue_AndCrmPeerNetworkSync_WorkEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (stewardId, constituentId) = await SeedStewardAndConstituentAsync(commands);
        await VerifyTouchpointCompletionAndCrmSyncAsync(
            commands,
            queries,
            stewardId,
            constituentId
        );
    }

    private static async Task<(Guid StewardId, Guid ConstituentId)> SeedStewardAndConstituentAsync(
        ICommandDispatcher commands
    )
    {
        var p1 = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Clara", "Steward", Email: "clara@example.org")
        );
        var p2 = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Marcus", "Hale", Email: "marcus@example.org")
        );
        return (p1.Value.Id, p2.Value.Id);
    }

    private static async Task VerifyTouchpointCompletionAndCrmSyncAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid stewardId,
        Guid constituentId
    )
    {
        var upserted = await commands.SendAsync<
            UpsertConstituentContactCommand,
            ConstituentContactResponse
        >(
            new UpsertConstituentContactCommand(
                new ConstituentContactSpec(
                    stewardId,
                    constituentId,
                    "Major Donor Steward",
                    OutreachCadenceDays: 30
                )
            )
        );
        Assert.True(upserted.IsSuccess);

        var queue = await queries.QueryAsync<
            GetMyPortfolioOutreachQueueQuery,
            PortfolioOutreachQueueResponse
        >(new GetMyPortfolioOutreachQueueQuery(stewardId));
        var dueTp = Assert.Single(queue.Value.OutreachQueue);

        var done = await commands.SendAsync<CompleteTouchpointCommand, TouchpointResponse>(
            new CompleteTouchpointCommand(
                dueTp.Id,
                TouchpointChannel.InPersonMeeting,
                TouchpointOutcomeSentiment.Positive,
                "Reviewed annual impact goals",
                ContactRelationshipStrength.Strong
            )
        );
        Assert.True(done.IsSuccess);

        var edges = await queries.QueryAsync<
            GetPersonPeerNetworkQuery,
            IReadOnlyList<PeerNetworkEdgeResponse>
        >(new GetPersonPeerNetworkQuery(stewardId));
        Assert.Contains(edges.Value, e =>
            e.TargetPersonId == constituentId && e.Strength == RelationshipStrength.Deep
        );
    }
}
