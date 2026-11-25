using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S6GroupRequirementsIntegrationTests(GroupsPostgresContainerFixture postgres)
{
    [Fact]
    public async Task Requirements_BackgroundCheck_PersonAttribute_GroupPrereq_AndOverride_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var (clearPersonId, missingPersonId) =
            await CrmGroupsTestSeeder.SeedBackgroundCheckPairAsync(commands);
        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Kids Ministry",
                InitialRoles: [new GroupTypeRoleInputDto("Volunteer")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);

        var prereqGroup = await CreateGroupAsync(
            commands,
            groupTypeResult.Value.Id,
            "Membership Class"
        );
        var kidsGroup = await CreateGroupAsync(commands, groupTypeResult.Value.Id, "Kids Room 1");

        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(prereqGroup.Id, clearPersonId)
                )
            ).IsSuccess
        );

        var manualReqId = await ConfigureKidsRequirementsAsync(
            commands,
            kidsGroup.Id,
            prereqGroup.Id
        );

        var blockedAdd = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(kidsGroup.Id, missingPersonId)
        );
        Assert.True(blockedAdd.IsFailure);

        var clearMember = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(kidsGroup.Id, clearPersonId)
        );
        Assert.True(clearMember.IsSuccess);

        var evaluated = await commands.SendAsync<
            EvaluateGroupMemberRequirementsCommand,
            IReadOnlyList<GroupMemberRequirementResponse>
        >(new EvaluateGroupMemberRequirementsCommand(clearMember.Value.Id));
        Assert.True(evaluated.IsSuccess);
        Assert.Equal(4, evaluated.Value.Count);
        Assert.Equal(3, evaluated.Value.Count(static r => r.State == MeetsGroupRequirement.Meets));
        _ = Assert.Single(evaluated.Value, static r => r.State == MeetsGroupRequirement.NotMet);

        var overridden = await commands.SendAsync<
            SetGroupMemberRequirementManualStateCommand,
            GroupMemberRequirementResponse
        >(
            new SetGroupMemberRequirementManualStateCommand(
                GroupMemberId: clearMember.Value.Id,
                GroupRequirementId: manualReqId,
                IsOverride: true,
                IsMet: true,
                ActorPersonId: clearPersonId,
                Note: "Verified verbally"
            )
        );
        Assert.True(overridden.IsSuccess);
        Assert.Equal(MeetsGroupRequirement.Meets, overridden.Value.State);
        Assert.True(overridden.Value.WasOverridden);
    }

    private static async Task<GroupDetailResponse> CreateGroupAsync(
        ICommandDispatcher commands,
        Guid groupTypeId,
        string name
    )
    {
        var result = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(GroupTypeId: groupTypeId, Name: name, IsPublic: true)
        );
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static async Task<Guid> ConfigureKidsRequirementsAsync(
        ICommandDispatcher commands,
        Guid kidsGroupId,
        Guid prereqGroupId
    )
    {
        var bgType = await commands.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(
            new CreateGroupRequirementTypeCommand(
                Name: "Kids Background Check",
                CheckType: RequirementCheckType.BackgroundCheckClear,
                RuleKey: "kids-volunteer",
                CanExpire: true,
                ExpireInDays: 730
            )
        );
        var attrType = await commands.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(
            new CreateGroupRequirementTypeCommand(
                Name: "Signed Covenant",
                CheckType: RequirementCheckType.PersonAttribute,
                RuleKey: "covenant_signed",
                RuleValue: "true"
            )
        );
        var prereqType = await commands.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(
            new CreateGroupRequirementTypeCommand(
                Name: "Completed Membership Class",
                CheckType: RequirementCheckType.GroupMembership,
                RuleKey: prereqGroupId.ToString("D")
            )
        );
        var manualType = await commands.SendAsync<
            CreateGroupRequirementTypeCommand,
            GroupRequirementTypeResponse
        >(
            new CreateGroupRequirementTypeCommand(
                Name: "Pastor Interview",
                CheckType: RequirementCheckType.Manual
            )
        );
        Assert.True(
            bgType.IsSuccess && attrType.IsSuccess && prereqType.IsSuccess && manualType.IsSuccess
        );

        _ = await commands.SendAsync<AddGroupRequirementCommand, GroupRequirementResponse>(
            new AddGroupRequirementCommand(
                bgType.Value.Id,
                GroupId: kidsGroupId,
                MustMeetRequirementToAddMember: true
            )
        );
        _ = await commands.SendAsync<AddGroupRequirementCommand, GroupRequirementResponse>(
            new AddGroupRequirementCommand(
                attrType.Value.Id,
                GroupId: kidsGroupId,
                MustMeetRequirementToAddMember: true
            )
        );
        _ = await commands.SendAsync<AddGroupRequirementCommand, GroupRequirementResponse>(
            new AddGroupRequirementCommand(
                prereqType.Value.Id,
                GroupId: kidsGroupId,
                MustMeetRequirementToAddMember: true
            )
        );
        var manualReq = await commands.SendAsync<
            AddGroupRequirementCommand,
            GroupRequirementResponse
        >(
            new AddGroupRequirementCommand(
                manualType.Value.Id,
                GroupId: kidsGroupId,
                MustMeetRequirementToAddMember: false,
                AllowLeadersToOverride: true
            )
        );
        Assert.True(manualReq.IsSuccess);
        return manualReq.Value.Id;
    }
}
