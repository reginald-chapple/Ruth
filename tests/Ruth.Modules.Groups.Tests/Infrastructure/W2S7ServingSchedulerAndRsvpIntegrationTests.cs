using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S7ServingSchedulerAndRsvpIntegrationTests(
    GroupsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ServingScheduler_Conflicts_AutoPopulate_Grid_Toolbox_AndRsvp_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var context = await ServingSchedulerTestSeeder.SeedServingTeamAsync(commands);
        await VerifySchedulingConflictsAndExclusionsAsync(commands, context);
        await VerifyAutoPopulateGridToolboxAndRsvpAsync(commands, queries, context);
    }

    private static async Task VerifySchedulingConflictsAndExclusionsAsync(
        ICommandDispatcher commands,
        ServingSetupContext context
    )
    {
        var blockedDate = new DateOnly(2026, 7, 12);
        var holidayDate = new DateOnly(2026, 7, 19);

        var unreadyBlocked = await commands.SendAsync<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse
        >(
            new ScheduleVolunteerCommand(
                GroupId: context.GroupId,
                PersonId: context.UnreadyPersonId,
                OccurrenceDate: blockedDate,
                LocationId: context.LocationId,
                ScheduleId: context.ScheduleId
            )
        );
        Assert.True(unreadyBlocked.IsFailure);

        var blackout = await commands.SendAsync<
            AddPersonScheduleExclusionCommand,
            PersonScheduleExclusionResponse
        >(
            new AddPersonScheduleExclusionCommand(
                PersonId: context.AlicePersonId,
                StartDate: blockedDate,
                EndDate: blockedDate,
                Title: "Summer Vacation"
            )
        );
        Assert.True(blackout.IsSuccess);

        var aliceBlackoutBlocked = await commands.SendAsync<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse
        >(
            new ScheduleVolunteerCommand(
                GroupId: context.GroupId,
                PersonId: context.AlicePersonId,
                OccurrenceDate: blockedDate,
                LocationId: context.LocationId,
                ScheduleId: context.ScheduleId
            )
        );
        Assert.True(aliceBlackoutBlocked.IsFailure);

        var groupExclusion = await commands.SendAsync<
            AddGroupScheduleExclusionCommand,
            GroupScheduleExclusionResponse
        >(
            new AddGroupScheduleExclusionCommand(
                GroupTypeId: context.GroupTypeId,
                StartDate: holidayDate,
                EndDate: holidayDate,
                Title: "Church Retreat"
            )
        );
        Assert.True(groupExclusion.IsSuccess);

        var holidayBlocked = await commands.SendAsync<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse
        >(
            new ScheduleVolunteerCommand(
                GroupId: context.GroupId,
                PersonId: context.BobPersonId,
                OccurrenceDate: holidayDate,
                LocationId: context.LocationId,
                ScheduleId: context.ScheduleId
            )
        );
        Assert.True(holidayBlocked.IsFailure);
    }

    private static async Task VerifyAutoPopulateGridToolboxAndRsvpAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        ServingSetupContext context
    )
    {
        var serviceDate = new DateOnly(2026, 7, 5);

        var autoPop = await commands.SendAsync<
            AutoPopulateServingScheduleCommand,
            AutoPopulateScheduleResponse
        >(new AutoPopulateServingScheduleCommand(context.GroupId, serviceDate));
        Assert.True(autoPop.IsSuccess);
        Assert.Equal(2, autoPop.Value.VolunteersScheduled);

        var secondGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(context.GroupTypeId, "Parking Team")
        );
        Assert.True(secondGroup.IsSuccess);
        _ = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(secondGroup.Value.Id, context.AlicePersonId)
        );
        var doubleBooked = await commands.SendAsync<
            ScheduleVolunteerCommand,
            ScheduledVolunteerResponse
        >(
            new ScheduleVolunteerCommand(
                GroupId: secondGroup.Value.Id,
                PersonId: context.AlicePersonId,
                OccurrenceDate: serviceDate,
                ScheduleId: context.ScheduleId
            )
        );
        Assert.True(doubleBooked.IsFailure);

        var aliceAssignment = Assert.Single(autoPop.Value.CreatedAssignments, a =>
            a.PersonId == context.AlicePersonId
        );
        var bobAssignment = Assert.Single(autoPop.Value.CreatedAssignments, a =>
            a.PersonId == context.BobPersonId
        );
        Assert.False(string.IsNullOrWhiteSpace(aliceAssignment.RsvpToken));

        var lookup = await queries.QueryAsync<GetRsvpByTokenQuery, ScheduledVolunteerResponse>(
            new GetRsvpByTokenQuery(aliceAssignment.RsvpToken)
        );
        Assert.True(lookup.IsSuccess);
        Assert.Equal(aliceAssignment.AttendanceId, lookup.Value.AttendanceId);

        var accept = await commands.SendAsync<RespondToRsvpCommand, ScheduledVolunteerResponse>(
            new RespondToRsvpCommand(
                AttendanceId: null,
                RsvpToken: aliceAssignment.RsvpToken,
                Response: RsvpStatus.Yes
            )
        );
        Assert.True(accept.IsSuccess);
        Assert.Equal(RsvpStatus.Yes, accept.Value.Rsvp);
        Assert.True(accept.Value.ScheduledToAttend);

        var decline = await commands.SendAsync<RespondToRsvpCommand, ScheduledVolunteerResponse>(
            new RespondToRsvpCommand(
                AttendanceId: null,
                RsvpToken: bobAssignment.RsvpToken,
                Response: RsvpStatus.No,
                DeclineReasonNote: "Out of town"
            )
        );
        Assert.True(decline.IsSuccess);
        Assert.Equal(RsvpStatus.No, decline.Value.Rsvp);
        Assert.False(decline.Value.ScheduledToAttend);

        var grid = await queries.QueryAsync<
            GetServingScheduleGridQuery,
            ServingScheduleGridResponse
        >(new GetServingScheduleGridQuery(context.GroupId, serviceDate, serviceDate));
        Assert.True(grid.IsSuccess);
        var slot = Assert.Single(grid.Value.Slots);
        Assert.Equal(1, slot.ConfirmedCount);
        Assert.Equal(1, slot.DeclinedCount);

        var toolbox = await queries.QueryAsync<
            GetVolunteerScheduleToolboxQuery,
            VolunteerScheduleToolboxResponse
        >(new GetVolunteerScheduleToolboxQuery(context.AlicePersonId, serviceDate));
        Assert.True(toolbox.IsSuccess);
        _ = Assert.Single(toolbox.Value.UpcomingAssignments);
        _ = Assert.Single(toolbox.Value.BlackoutDates);
    }
}
