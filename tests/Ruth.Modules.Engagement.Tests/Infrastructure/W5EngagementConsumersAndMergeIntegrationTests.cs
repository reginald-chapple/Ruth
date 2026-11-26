using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W5EngagementConsumersAndMergeIntegrationTests(
    EngagementPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ConnectedAchievementConsumer_AndPersonMergeBitwiseStreakMerge_WorkIdempotently()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var stewardId = Guid.CreateVersion7();
        var survivingId = Guid.CreateVersion7();
        var retiredId = Guid.CreateVersion7();

        await VerifyConnectedAchievementConsumerAsync(
            scope.ServiceProvider,
            commands,
            queries,
            survivingId
        );
        await VerifyPersonMergeAsync(
            scope.ServiceProvider,
            commands,
            queries,
            stewardId,
            survivingId,
            retiredId
        );
    }

    private static async Task VerifyConnectedAchievementConsumerAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        _ = await commands.SendAsync<CreateAchievementTypeCommand, AchievementTypeResponse>(
            new CreateAchievementTypeCommand(
                new AchievementTypeSpec(
                    "Connected Volunteer",
                    "CONNECTED_VOLUNTEER",
                    AchievementSourceKind.EngagementConnections,
                    TargetCount: 1
                )
            )
        );

        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<EngagementRequestConnectedIntegrationEvent>
        >();
        await consumer.ConsumeAsync(
            new EngagementRequestConnectedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                personId,
                AssignedConnectorPersonId: null,
                PlacementGroupId: null
            )
        );

        var attempts = await queries.QueryAsync<
            GetAchievementAttemptsQuery,
            IReadOnlyList<AchievementAttemptResponse>
        >(new GetAchievementAttemptsQuery(PersonId: personId, SuccessfulOnly: true));
        _ = Assert.Single(attempts.Value);
    }

    private static async Task VerifyPersonMergeAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid stewardId,
        Guid survivingId,
        Guid retiredId
    )
    {
        var anchor = new DateOnly(2026, 2, 1);
        var st = await commands.SendAsync<CreateStreakTypeCommand, StreakTypeResponse>(
            new CreateStreakTypeCommand(
                new StreakTypeSpec("Daily Check-In", "DAILY_CHECK", StreakCadence.Daily, anchor)
            )
        );
        _ = await commands.SendAsync<RecordStreakEngagementCommand, StreakResponse>(
            new RecordStreakEngagementCommand(st.Value.Id, survivingId, anchor)
        );
        _ = await commands.SendAsync<RecordStreakEngagementCommand, StreakResponse>(
            new RecordStreakEngagementCommand(st.Value.Id, retiredId, anchor.AddDays(1))
        );
        _ = await commands.SendAsync<UpsertConstituentContactCommand, ConstituentContactResponse>(
            new UpsertConstituentContactCommand(new ConstituentContactSpec(stewardId, retiredId))
        );

        var mergeConsumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        var evt = new PersonMergedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            survivingId,
            retiredId,
            [],
            HadMultipleDistinctEmails: true
        );
        await mergeConsumer.ConsumeAsync(evt);
        await mergeConsumer.ConsumeAsync(evt);

        var streaks = await queries.QueryAsync<GetStreaksQuery, IReadOnlyList<StreakResponse>>(
            new GetStreaksQuery(st.Value.Id, survivingId)
        );
        var mergedStreak = Assert.Single(streaks.Value);
        Assert.Equal(2, mergedStreak.EngagementCount);
        Assert.Equal(2, mergedStreak.LongestStreakCount);

        var retiredStreaks = await queries.QueryAsync<
            GetStreaksQuery,
            IReadOnlyList<StreakResponse>
        >(new GetStreaksQuery(st.Value.Id, retiredId));
        Assert.Empty(retiredStreaks.Value);
    }
}
