using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class CommerceConstituentSeedHelper
{
    private static readonly Dictionary<string, string?> _goldTierAttributes = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ["MembershipTier"] = "Gold",
    };

    public static async Task<(
        Guid GroupTypeId,
        Guid GroupId,
        Guid MemberId,
        Guid PartnerId
    )> SeedPeopleHouseholdAndGroupAsync(ICommandDispatcher commands)
    {
        var (groupTypeId, groupId) = await SeedGroupAsync(commands);
        var (memberId, partnerId) = await SeedHouseholdAsync(commands);

        var gm = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(groupId, memberId, SkipRequirementsCheck: true)
        );
        Assert.True(gm.IsSuccess);

        return (groupTypeId, groupId, memberId, partnerId);
    }

    private static async Task<(Guid GroupTypeId, Guid GroupId)> SeedGroupAsync(
        ICommandDispatcher commands
    )
    {
        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand("Sustaining Members")
        );
        Assert.True(gt.IsSuccess);

        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Sustaining Chapter")
        );
        Assert.True(grp.IsSuccess);
        return (gt.Value.Id, grp.Value.Id);
    }

    private static async Task<(Guid MemberId, Guid PartnerId)> SeedHouseholdAsync(
        ICommandDispatcher commands
    )
    {
        var member = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                "Nora",
                "Chen",
                Email: "nora.chen@example.org",
                Attributes: _goldTierAttributes
            )
        );
        Assert.True(member.IsSuccess);

        var partner = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Liam", "Chen", Email: "liam.chen@example.org")
        );
        Assert.True(partner.IsSuccess);

        var hh = await commands.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
            new CreateHouseholdCommand(
                "Chen Household",
                Members:
                [
                    new HouseholdMemberInputDto(member.Value.Id, IsPrimaryContact: true),
                    new HouseholdMemberInputDto(partner.Value.Id),
                ]
            )
        );
        Assert.True(hh.IsSuccess);
        return (member.Value.Id, partner.Value.Id);
    }
}
