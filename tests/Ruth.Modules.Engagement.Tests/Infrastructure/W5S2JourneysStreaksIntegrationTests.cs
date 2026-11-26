using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Tests.Fixtures;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Infrastructure;

[Collection(EngagementPostgresFixtures.Name)]
public sealed class W5S2JourneysStreaksIntegrationTests(EngagementPostgresContainerFixture postgres)
{
    [Fact]
    public async Task Journeys_Streaks_AttendanceEvents_AndAchievements_PersistAndEvaluateEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EngagementTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var personId = Guid.CreateVersion7();
        await VerifyJourneyMilestonesAndAchievementsAsync(commands, queries, personId);
        await VerifyAttendanceDrivenStreaksAndAchievementsAsync(
            scope.ServiceProvider,
            commands,
            queries,
            personId
        );
    }

    private static async Task VerifyJourneyMilestonesAndAchievementsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        var progRes = await commands.SendAsync<CreateJourneyProgramCommand, JourneyProgramResponse>(
            new CreateJourneyProgramCommand(
                new JourneyProgramSpec(
                    "Leadership Pathway",
                    "leadership-pathway",
                    [
                        new MilestoneStatusSpec(
                            "In Progress",
                            "#0F6CBD",
                            IsCompleteStatus: false,
                            Order: 0
                        ),
                        new MilestoneStatusSpec(
                            "Completed",
                            "#107C10",
                            IsCompleteStatus: true,
                            Order: 1
                        ),
                    ],
                    [
                        new MilestoneTypeSpec("Orientation", "ORIENT", 0),
                        new MilestoneTypeSpec(
                            "Shadow Shift",
                            "SHADOW",
                            1,
                            PrerequisiteCodes: ["ORIENT"]
                        ),
                    ]
                )
            )
        );
        Assert.True(progRes.IsSuccess);
        var prog = progRes.Value;

        _ = await commands.SendAsync<CreateAchievementTypeCommand, AchievementTypeResponse>(
            new CreateAchievementTypeCommand(
                new AchievementTypeSpec(
                    "Pathway Graduate",
                    "PATHWAY_GRAD",
                    AchievementSourceKind.JourneyCompletions,
                    TargetCount: 1,
                    TargetJourneyProgramId: prog.Id
                )
            )
        );

        var doneId = prog.Statuses.First(static s => s.IsCompleteStatus).Id;
        var orientId = prog.MilestoneTypes[0].Id;
        var shadowId = prog.MilestoneTypes[1].Id;

        Assert.True(
            (
                await commands.SendAsync<RecordPersonMilestoneCommand, PersonMilestoneResponse>(
                    new RecordPersonMilestoneCommand(
                        personId,
                        prog.Id,
                        orientId,
                        doneId,
                        new DateOnly(2026, 11, 1)
                    )
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await commands.SendAsync<RecordPersonMilestoneCommand, PersonMilestoneResponse>(
                    new RecordPersonMilestoneCommand(
                        personId,
                        prog.Id,
                        shadowId,
                        doneId,
                        new DateOnly(2026, 11, 10)
                    )
                )
            ).IsSuccess
        );

        var comps = await queries.QueryAsync<
            GetJourneyCompletionsQuery,
            IReadOnlyList<JourneyCompletionResponse>
        >(new GetJourneyCompletionsQuery(prog.Id, personId));
        _ = Assert.Single(comps.Value);
    }

    private static async Task VerifyAttendanceDrivenStreaksAndAchievementsAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        var anchor = new DateOnly(2026, 1, 4);
        var stRes = await commands.SendAsync<CreateStreakTypeCommand, StreakTypeResponse>(
            new CreateStreakTypeCommand(
                new StreakTypeSpec(
                    "Weekly Community Gathering",
                    "WEEKLY_GATHER",
                    StreakCadence.Weekly,
                    anchor,
                    SourceKind: StreakSourceKind.AnyAttendance
                )
            )
        );
        Assert.True(stRes.IsSuccess);
        var stId = stRes.Value.Id;

        _ = await commands.SendAsync<AddStreakExclusionCommand, StreakTypeResponse>(
            new AddStreakExclusionCommand(stId, new DateOnly(2026, 1, 18))
        );
        _ = await commands.SendAsync<CreateAchievementTypeCommand, AchievementTypeResponse>(
            new CreateAchievementTypeCommand(
                new AchievementTypeSpec(
                    "3-Week Streak",
                    "STREAK_3",
                    AchievementSourceKind.StreakCount,
                    TargetCount: 3,
                    TargetStreakTypeId: stId
                )
            )
        );

        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonAttendedIntegrationEvent>
        >();
        foreach (
            var date in new[]
            {
                new DateOnly(2026, 1, 4),
                new DateOnly(2026, 1, 11),
                new DateOnly(2026, 1, 25),
            }
        )
        {
            await consumer.ConsumeAsync(
                new PersonAttendedIntegrationEvent(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    personId,
                    GroupId: null,
                    GroupTypeId: null,
                    BranchId: null,
                    LocationId: null,
                    ScheduleId: null,
                    date,
                    date,
                    IsFirstTime: false,
                    CountsTowardHeadlineAttendance: true
                )
            );
        }

        var streaks = await queries.QueryAsync<GetStreaksQuery, IReadOnlyList<StreakResponse>>(
            new GetStreaksQuery(stId, personId)
        );
        var singleStreak = Assert.Single(streaks.Value);
        Assert.Equal(3, singleStreak.LongestStreakCount);
    }
}
