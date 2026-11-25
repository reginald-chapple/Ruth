using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S8GroupAttendanceAndMetricsIntegrationTests(
    GroupsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task GroupAttendance_FirstTimeDetection_OutboxEvent_AndMetricsSummary_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<GroupsDbContext>();

        var campusId = Guid.CreateVersion7();
        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Weekend Service Team",
                TakesAttendance: true,
                AttendanceCountsAsWeekendService: true,
                InitialRoles: [new GroupTypeRoleInputDto("Attendee")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);

        var groupResult = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: groupTypeResult.Value.Id,
                Name: "Main Auditorium 9AM",
                CampusId: campusId
            )
        );
        Assert.True(groupResult.IsSuccess);

        var personA = Guid.CreateVersion7();
        var personB = Guid.CreateVersion7();

        await RecordTwoWeeksAndDidNotOccurAsync(commands, groupResult.Value.Id, personA, personB);
        await VerifyOutboxEventsAndMetricsAsync(
            queries,
            dbContext,
            campusId,
            groupResult.Value.Id,
            personA
        );
    }

    private static async Task RecordTwoWeeksAndDidNotOccurAsync(
        ICommandDispatcher commands,
        Guid groupId,
        Guid personA,
        Guid personB
    )
    {
        var week1 = await commands.SendAsync<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse
        >(
            new RecordGroupAttendanceCommand(
                GroupId: groupId,
                OccurrenceDate: new DateOnly(2026, 8, 2),
                AnonymousAttendanceCount: 5,
                Entries:
                [
                    new GroupAttendanceEntryInputDto(personA, DidAttend: true),
                    new GroupAttendanceEntryInputDto(personB, DidAttend: false),
                ]
            )
        );
        Assert.True(week1.IsSuccess);
        Assert.Equal(1, week1.Value.PresentCount);
        Assert.Equal(1, week1.Value.FirstTimeCount);
        Assert.True(
            week1.Value.Attendances.Single(a => a.PersonId == personA).IsFirstTime ?? false
        );

        var week2 = await commands.SendAsync<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse
        >(
            new RecordGroupAttendanceCommand(
                GroupId: groupId,
                OccurrenceDate: new DateOnly(2026, 8, 9),
                AnonymousAttendanceCount: 3,
                Entries:
                [
                    new GroupAttendanceEntryInputDto(personA, DidAttend: true),
                    new GroupAttendanceEntryInputDto(personB, DidAttend: true),
                ]
            )
        );
        Assert.True(week2.IsSuccess);
        Assert.Equal(2, week2.Value.PresentCount);
        Assert.Equal(1, week2.Value.FirstTimeCount);
        Assert.False(
            week2.Value.Attendances.Single(a => a.PersonId == personA).IsFirstTime ?? true
        );
        Assert.True(
            week2.Value.Attendances.Single(a => a.PersonId == personB).IsFirstTime ?? false
        );

        var week3 = await commands.SendAsync<
            RecordGroupAttendanceCommand,
            AttendanceOccurrenceDetailResponse
        >(
            new RecordGroupAttendanceCommand(
                GroupId: groupId,
                OccurrenceDate: new DateOnly(2026, 8, 16),
                DidNotOccur: true,
                Notes: "Building maintenance"
            )
        );
        Assert.True(week3.IsSuccess);
        Assert.True(week3.Value.DidNotOccur);
    }

    private static async Task VerifyOutboxEventsAndMetricsAsync(
        IQueryDispatcher queries,
        GroupsDbContext dbContext,
        Guid campusId,
        Guid groupId,
        Guid personA
    )
    {
        var outboxRows = await dbContext
            .OutboxMessages.AsNoTracking()
            .Where(m => m.EventType.Contains(nameof(PersonAttendedIntegrationEvent)))
            .ToListAsync();
        Assert.Equal(3, outboxRows.Count);
        Assert.Contains(outboxRows, m =>
            m.Payload.Contains(personA.ToString("D"), StringComparison.OrdinalIgnoreCase)
        );

        var metrics = await queries.QueryAsync<
            GetAttendanceMetricsSummaryQuery,
            AttendanceMetricsSummaryResponse
        >(
            new GetAttendanceMetricsSummaryQuery(
                StartDate: new DateOnly(2026, 8, 1),
                EndDate: new DateOnly(2026, 8, 31),
                CampusId: campusId,
                WeekendServicesOnly: true
            )
        );
        Assert.True(metrics.IsSuccess);
        Assert.Equal(3, metrics.Value.TotalKnownAttendance);
        Assert.Equal(8, metrics.Value.TotalAnonymousAttendance);
        Assert.Equal(11, metrics.Value.TotalAttendance);
        Assert.Equal(2, metrics.Value.TotalUniquePeople);
        Assert.Equal(2, metrics.Value.TotalFirstTimeAttendees);
        Assert.Equal(11, metrics.Value.WeekendServiceTotalAttendance);
        Assert.Equal(3, metrics.Value.Buckets.Count);

        var occurrences = await queries.QueryAsync<
            GetGroupAttendanceOccurrencesQuery,
            IReadOnlyList<AttendanceOccurrenceSummaryResponse>
        >(new GetGroupAttendanceOccurrencesQuery(groupId));
        Assert.True(occurrences.IsSuccess);
        Assert.Equal(3, occurrences.Value.Count);
    }
}
