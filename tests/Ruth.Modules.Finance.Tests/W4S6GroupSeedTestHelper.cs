using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Tests;

internal static class W4S6GroupSeedTestHelper
{
    public static async Task<(
        Guid GroupId,
        Guid MemberAId,
        Guid MemberBId
    )> CreateFundraisingTeamAsync(ICommandDispatcher commands)
    {
        var personA = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Maya", "Lin", Email: "maya@ruth.test")
            )
        ).Value;
        var personB = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Devon", "Brooks", Email: "devon@ruth.test")
            )
        ).Value;

        var groupType = (
            await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
                new CreateGroupTypeCommand("Project Fundraising Teams")
            )
        ).Value;

        var group = (
            await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
                new CreateGroupCommand(groupType.Id, "Clean Water Expedition Team")
            )
        ).Value;

        _ = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(group.Id, personA.Id, SkipRequirementsCheck: true)
        );
        _ = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(group.Id, personB.Id, SkipRequirementsCheck: true)
        );

        return (group.Id, personA.Id, personB.Id);
    }
}
