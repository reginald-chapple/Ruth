using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Fixtures;

internal static class ServingSchedulerTestSeeder
{
    public static async Task<ServingSetupContext> SeedServingTeamAsync(ICommandDispatcher commands)
    {
        var scheduleId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();

        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Serving Team",
                IsSchedulingEnabled: true,
                EnableLocationSchedules: true,
                TakesAttendance: true,
                InitialRoles: [new GroupTypeRoleInputDto("Volunteer")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);

        var templateResult = await commands.SendAsync<
            CreateScheduleTemplateCommand,
            GroupMemberScheduleTemplateResponse
        >(
            new CreateScheduleTemplateCommand(
                Name: "Weekly Rotation",
                ScheduleId: scheduleId,
                GroupTypeId: groupTypeResult.Value.Id,
                RecurrenceDescription: "Every Week",
                IntervalWeeks: 1
            )
        );
        Assert.True(templateResult.IsSuccess);

        var groupResult = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: groupTypeResult.Value.Id,
                Name: "Welcome Team",
                SchedulingMustMeetRequirements: true
            )
        );
        Assert.True(groupResult.IsSuccess);

        var attachLoc = await commands.SendAsync<AttachGroupLocationCommand, GroupDetailResponse>(
            new AttachGroupLocationCommand(
                GroupId: groupResult.Value.Id,
                LocationId: locationId,
                LocationName: "Main Lobby",
                ScheduleIds: [scheduleId]
            )
        );
        Assert.True(attachLoc.IsSuccess);
        var groupLocationId = attachLoc.Value.Locations[0].Id;

        var configResult = await commands.SendAsync<
            UpsertLocationScheduleConfigCommand,
            GroupLocationScheduleConfigResponse
        >(
            new UpsertLocationScheduleConfigCommand(
                GroupLocationId: groupLocationId,
                ScheduleId: scheduleId,
                ConfigurationName: "9AM Lobby",
                MinimumCapacity: 1,
                DesiredCapacity: 2,
                MaximumCapacity: 3
            )
        );
        Assert.True(configResult.IsSuccess);

        return await SeedVolunteersAndRequirementAsync(
            commands,
            groupTypeResult.Value.Id,
            groupResult.Value.Id,
            locationId,
            scheduleId,
            templateResult.Value.Id
        );
    }

    private static async Task<ServingSetupContext> SeedVolunteersAndRequirementAsync(
        ICommandDispatcher commands,
        Guid groupTypeId,
        Guid groupId,
        Guid locationId,
        Guid scheduleId,
        Guid templateId
    )
    {
        var reqTypeResult = await commands.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(
            new CreateGroupRequirementTypeCommand(
                Name: "Serve Orientation",
                CheckType: RequirementCheckType.Manual
            )
        );
        Assert.True(reqTypeResult.IsSuccess);

        var reqResult = await commands.SendAsync<
            AddGroupRequirementCommand,
            GroupRequirementResponse
        >(
            new AddGroupRequirementCommand(
                GroupRequirementTypeId: reqTypeResult.Value.Id,
                GroupId: groupId,
                MustMeetRequirementToAddMember: false
            )
        );
        Assert.True(reqResult.IsSuccess);

        var alicePersonId = Guid.CreateVersion7();
        var bobPersonId = Guid.CreateVersion7();
        var unreadyPersonId = Guid.CreateVersion7();

        var aliceMember = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(
                GroupId: groupId,
                PersonId: alicePersonId,
                ScheduleTemplateId: templateId,
                ScheduleStartDate: new DateOnly(2026, 7, 5)
            )
        );
        var bobMember = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(
                GroupId: groupId,
                PersonId: bobPersonId,
                ScheduleTemplateId: templateId,
                ScheduleStartDate: new DateOnly(2026, 7, 5)
            )
        );
        var unreadyMember = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(GroupId: groupId, PersonId: unreadyPersonId)
        );
        Assert.True(aliceMember.IsSuccess && bobMember.IsSuccess && unreadyMember.IsSuccess);

        foreach (var memberId in new[] { aliceMember.Value.Id, bobMember.Value.Id })
        {
            var setMet = await commands.SendAsync<
                SetGroupMemberRequirementManualStateCommand,
                GroupMemberRequirementResponse
            >(
                new SetGroupMemberRequirementManualStateCommand(
                    GroupMemberId: memberId,
                    GroupRequirementId: reqResult.Value.Id,
                    IsOverride: false,
                    IsMet: true
                )
            );
            Assert.True(setMet.IsSuccess);
        }

        var assignAlice = await commands.SendAsync<
            UpsertGroupMemberAssignmentCommand,
            GroupMemberAssignmentResponse
        >(
            new UpsertGroupMemberAssignmentCommand(
                GroupMemberId: aliceMember.Value.Id,
                LocationId: locationId,
                ScheduleId: scheduleId
            )
        );
        Assert.True(assignAlice.IsSuccess);

        return new ServingSetupContext(
            groupTypeId,
            groupId,
            locationId,
            scheduleId,
            alicePersonId,
            bobPersonId,
            unreadyPersonId
        );
    }
}
