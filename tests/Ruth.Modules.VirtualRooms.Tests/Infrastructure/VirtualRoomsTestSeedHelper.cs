using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;
using Xunit;

namespace Ruth.Modules.VirtualRooms.Tests.Infrastructure;

internal static class VirtualRoomsTestSeedHelper
{
    public static async Task<(
        Guid GroupId,
        Guid HostPersonId,
        Guid AttendeePersonId
    )> SeedGroupAndPeopleAsync(ICommandDispatcher commands)
    {
        var host = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Clara", "Barton", Email: "clara.barton@example.org")
        );
        var attendee = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Devon", "Brooks", Email: "devon.brooks@example.org")
        );

        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand(
                "Hybrid Cohorts",
                TakesAttendance: true,
                InitialRoles: [new GroupTypeRoleInputDto("Participant", IsLeader: false)]
            )
        );
        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Tuesday Hybrid Cohort")
        );

        return (grp.Value.Id, host.Value.Id, attendee.Value.Id);
    }

    public static async Task<VirtualRoomResponse> CreateGroupVirtualRoomAsync(
        ICommandDispatcher commands,
        Guid groupId,
        Guid hostPersonId
    )
    {
        var res = await commands.SendAsync<CreateVirtualRoomCommand, VirtualRoomResponse>(
            new CreateVirtualRoomCommand(
                new VirtualRoomSpec(
                    "Tuesday Hybrid Cohort Stage",
                    "tuesday-hybrid-cohort-stage",
                    VirtualRoomContextKind.GroupMeeting,
                    MinimumAttendanceDwellSeconds: 300,
                    AutoRecordAttendance: true,
                    LinkedGroupId: groupId,
                    HostModeratorPersonId: hostPersonId
                )
            )
        );
        Assert.True(res.IsSuccess);
        return res.Value;
    }

    public static async Task VerifyAclTokenMintingAsync(
        ICommandDispatcher commands,
        VirtualRoomResponse room,
        Guid hostPersonId,
        Guid attendeePersonId
    )
    {
        var hostToken = await commands.SendAsync<
            MintVirtualRoomJoinTokenCommand,
            VirtualRoomJoinSessionResponse
        >(
            new MintVirtualRoomJoinTokenCommand(
                room.Slug,
                "Host Leader",
                PersonId: hostPersonId,
                Subject: new SecuritySubject(Guid.CreateVersion7(), new HashSet<Guid>())
            )
        );
        Assert.True(hostToken.IsSuccess);
        Assert.Equal(VirtualParticipantRole.HostModerator, hostToken.Value.GrantedRole);

        var attendeeToken = await commands.SendAsync<
            MintVirtualRoomJoinTokenCommand,
            VirtualRoomJoinSessionResponse
        >(
            new MintVirtualRoomJoinTokenCommand(
                room.Slug,
                "Attendee One",
                PersonId: attendeePersonId,
                Subject: new SecuritySubject(Guid.CreateVersion7(), new HashSet<Guid>())
            )
        );
        Assert.True(attendeeToken.IsSuccess);
        Assert.Equal(VirtualParticipantRole.Participant, attendeeToken.Value.GrantedRole);

        _ = await commands.SendAsync<
            SaveEntityAclRulesCommand,
            IReadOnlyList<EntityAclRuleResponse>
        >(
            new SaveEntityAclRulesCommand(
                VirtualRoom.EntityTypeKeyName,
                room.Id,
                AuthorizationActions.View,
                [new AclRuleInput(AclRuleDecision.Deny, SpecialRoleType.AllUnauthenticatedUsers)]
            )
        );

        var deniedToken = await commands.SendAsync<
            MintVirtualRoomJoinTokenCommand,
            VirtualRoomJoinSessionResponse
        >(
            new MintVirtualRoomJoinTokenCommand(
                room.Slug,
                "Blocked Guest",
                Subject: SecuritySubject.Anonymous
            )
        );
        Assert.True(deniedToken.IsFailure);
        Assert.Equal("VirtualRooms.JoinAccessForbidden", deniedToken.PrimaryError.Code);
    }
}
