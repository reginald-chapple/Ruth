using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Slices.Sync;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S6GroupSyncIntegrationTests(GroupsPostgresContainerFixture postgres)
{
    [Fact]
    public async Task GroupSyncJob_ReconcilesEntitySet_CrmConnectionStatus_AndSourceGroupMembers()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (entitySetTargetId, crmStatusTargetId, groupCopyTargetId, memberPerson) =
            await SeedSyncScenarioAsync(commands);

        var job = scope
            .ServiceProvider.GetServices<IScheduledJob>()
            .Single(static j =>
                string.Equals(j.JobKey, GroupSyncReconciliationJob.Key, StringComparison.Ordinal)
            );
        var jobResult = await job.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobResult.Status);

        await AssertSyncedRostersAsync(
            queries,
            entitySetTargetId,
            crmStatusTargetId,
            groupCopyTargetId,
            memberPerson
        );
    }

    private static async Task<(
        Guid EntitySetTargetId,
        Guid CrmStatusTargetId,
        Guid GroupCopyTargetId,
        Guid MemberPersonId
    )> SeedSyncScenarioAsync(ICommandDispatcher commands)
    {
        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Sync Groups",
                InitialRoles: [new GroupTypeRoleInputDto("Member")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);
        var groupTypeId = groupTypeResult.Value.Id;
        var roleId = groupTypeResult.Value.DefaultGroupRoleId!.Value;

        var memberPerson = await CrmGroupsTestSeeder.CreatePersonAsync(
            commands,
            "Liam",
            "Carter",
            PersonConnectionStatus.Member
        );
        var visitorPerson = await CrmGroupsTestSeeder.CreatePersonAsync(
            commands,
            "Noah",
            "Diaz",
            PersonConnectionStatus.Visitor
        );

        var sourceGroup = await CreateGroupAsync(commands, groupTypeId, "Source Roster");
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(sourceGroup.Id, memberPerson, roleId)
                )
            ).IsSuccess
        );

        var entitySet = await commands.SendAsync<CreateEntitySetCommand, EntitySetResponse>(
            new CreateEntitySetCommand(
                Name: "Sync Cohort",
                EntityTypeId: null,
                EntityIds: [memberPerson, visitorPerson]
            )
        );
        Assert.True(entitySet.IsSuccess);

        var entitySetTarget = await CreateGroupAsync(commands, groupTypeId, "EntitySet Sync");
        var crmStatusTarget = await CreateGroupAsync(commands, groupTypeId, "CrmStatus Sync");
        var groupCopyTarget = await CreateGroupAsync(commands, groupTypeId, "GroupCopy Sync");

        var stalePersonId = Guid.CreateVersion7();
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(crmStatusTarget.Id, stalePersonId, roleId)
                )
            ).IsSuccess
        );

        await ConfigureSyncRulesAsync(
            commands,
            roleId,
            entitySetTarget.Id,
            entitySet.Value.Id,
            crmStatusTarget.Id,
            groupCopyTarget.Id,
            sourceGroup.Id
        );

        return (entitySetTarget.Id, crmStatusTarget.Id, groupCopyTarget.Id, memberPerson);
    }

    private static async Task AssertSyncedRostersAsync(
        IQueryDispatcher queries,
        Guid entitySetTargetId,
        Guid crmStatusTargetId,
        Guid groupCopyTargetId,
        Guid memberPersonId
    )
    {
        var esMembers = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(entitySetTargetId));
        Assert.Equal(2, esMembers.Value.Count);

        var crmMembers = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(crmStatusTargetId));
        Assert.Equal(memberPersonId, Assert.Single(crmMembers.Value).PersonId);

        var copyMembers = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(groupCopyTargetId));
        Assert.Equal(memberPersonId, Assert.Single(copyMembers.Value).PersonId);
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

    private static async Task ConfigureSyncRulesAsync(
        ICommandDispatcher commands,
        Guid roleId,
        Guid entitySetTargetId,
        Guid entitySetId,
        Guid crmStatusTargetId,
        Guid groupCopyTargetId,
        Guid sourceGroupId
    )
    {
        _ = await commands.SendAsync<UpsertGroupSyncRuleCommand, GroupSyncRuleResponse>(
            new UpsertGroupSyncRuleCommand(
                entitySetTargetId,
                roleId,
                GroupSyncSourceType.EntitySet,
                SourceReferenceId: entitySetId,
                ScheduleIntervalMinutes: 1
            )
        );
        _ = await commands.SendAsync<UpsertGroupSyncRuleCommand, GroupSyncRuleResponse>(
            new UpsertGroupSyncRuleCommand(
                crmStatusTargetId,
                roleId,
                GroupSyncSourceType.CrmConnectionStatus,
                SourceFilterValue: "Member",
                ScheduleIntervalMinutes: 1
            )
        );
        _ = await commands.SendAsync<UpsertGroupSyncRuleCommand, GroupSyncRuleResponse>(
            new UpsertGroupSyncRuleCommand(
                groupCopyTargetId,
                roleId,
                GroupSyncSourceType.GroupMembers,
                SourceReferenceId: sourceGroupId,
                ScheduleIntervalMinutes: 1
            )
        );
    }
}
