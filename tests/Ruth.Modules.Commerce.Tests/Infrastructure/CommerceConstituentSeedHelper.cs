using Ruth.Modules.CheckIn.Contracts.WalletPasses;
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
    public static async Task<(
        Guid GroupTypeId,
        Guid GroupId,
        Guid MemberId,
        Guid PartnerId,
        Guid WalletPassId
    )> SeedPeopleHouseholdGroupAndWalletAsync(ICommandDispatcher commands)
    {
        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand("Sustaining Members")
        );
        Assert.True(gt.IsSuccess);

        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Sustaining Chapter")
        );
        Assert.True(grp.IsSuccess);

        var member = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Nora", "Chen", Email: "nora.chen@example.org")
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

        var gm = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(grp.Value.Id, member.Value.Id, SkipRequirementsCheck: true)
        );
        Assert.True(gm.IsSuccess);

        var walletId = await SeedMemberWalletPassAsync(commands, member.Value.Id);
        return (gt.Value.Id, grp.Value.Id, member.Value.Id, partner.Value.Id, walletId);
    }

    private static async Task<Guid> SeedMemberWalletPassAsync(
        ICommandDispatcher commands,
        Guid memberPersonId
    )
    {
        var tmpl = await commands.SendAsync<
            CreateWalletPassTemplateCommand,
            WalletPassTemplateResponse
        >(
            new CreateWalletPassTemplateCommand(
                "Gold Member Smart Card",
                $"commerce-gold-{Guid.NewGuid():N}"[..24],
                "pass.org.ruth.commerce.member",
                "Ruth Community",
                "Smart member and punch credential"
            )
        );
        Assert.True(tmpl.IsSuccess);

        var issued = await commands.SendAsync<
            IssueWalletPassCommand,
            IssueWalletPassResultResponse
        >(
            new IssueWalletPassCommand(
                tmpl.Value.Id,
                memberPersonId,
                new WalletPassDynamicFieldsDto(
                    HolderDisplayName: "Nora Chen",
                    MembershipTier: "Gold",
                    PunchOrCreditBalance: 0
                )
            )
        );
        Assert.True(issued.IsSuccess);
        return issued.Value.Pass.Id;
    }
}
