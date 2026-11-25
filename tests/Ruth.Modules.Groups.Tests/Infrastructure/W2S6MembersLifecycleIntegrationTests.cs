using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S6MembersLifecycleIntegrationTests(GroupsPostgresContainerFixture postgres)
{
    [Fact]
    public async Task MemberLifecycle_CapacityRules_RoleMaxCount_AndHistory_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var softType = await CreateGroupTypeAsync(
            commands,
            "Soft Capacity Team",
            GroupCapacityRule.Soft,
            leaderMaxCount: 1
        );
        var softGroup = await CreateGroupAsync(commands, softType.Id, "Soft Group", capacity: 1);
        var leaderRoleId = softType.Roles.Single(static r => r.IsLeader).Id;
        var memberRoleId = softType.Roles.Single(static r => !r.IsLeader).Id;

        var p1 = Guid.CreateVersion7();
        var p2 = Guid.CreateVersion7();

        var m1 = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(softGroup.Id, p1, leaderRoleId)
        );
        Assert.True(m1.IsSuccess);
        Assert.Null(m1.Value.CapacityWarning);

        var secondLeaderBlocked = await commands.SendAsync<
            AddGroupMemberCommand,
            GroupMemberResponse
        >(new AddGroupMemberCommand(softGroup.Id, p2, leaderRoleId));
        Assert.True(secondLeaderBlocked.IsFailure);

        var m2 = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(softGroup.Id, p2, memberRoleId)
        );
        Assert.True(m2.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(m2.Value.CapacityWarning));

        var updated = await commands.SendAsync<UpdateGroupMemberCommand, GroupMemberResponse>(
            new UpdateGroupMemberCommand(m2.Value.Id, memberRoleId, GroupMemberStatus.Inactive)
        );
        Assert.True(updated.IsSuccess);

        var archived = await commands.SendAsync<RemoveGroupMemberCommand, bool>(
            new RemoveGroupMemberCommand(m2.Value.Id, ArchiveInsteadOfDelete: true)
        );
        Assert.True(archived.IsSuccess);

        var history = await queries.QueryAsync<
            GetGroupMemberHistoryQuery,
            IReadOnlyList<GroupMemberHistoricalResponse>
        >(new GetGroupMemberHistoryQuery(m2.Value.Id));
        Assert.True(history.IsSuccess);
        Assert.True(history.Value.Count >= 3);
        Assert.True(history.Value.Single(static h => h.CurrentRowIndicator).IsArchived);

        var hardType = await CreateGroupTypeAsync(
            commands,
            "Hard Capacity Team",
            GroupCapacityRule.Hard,
            leaderMaxCount: null
        );
        var hardGroup = await CreateGroupAsync(commands, hardType.Id, "Hard Group", capacity: 1);
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(hardGroup.Id, p1)
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(hardGroup.Id, p2)
                )
            ).IsFailure
        );
    }

    private static async Task<GroupTypeDetailResponse> CreateGroupTypeAsync(
        ICommandDispatcher commands,
        string name,
        GroupCapacityRule capacityRule,
        int? leaderMaxCount
    )
    {
        var result = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand(
                Name: name,
                EnableGroupHistory: true,
                GroupCapacityRule: capacityRule,
                InitialRoles:
                [
                    new GroupTypeRoleInputDto("Member"),
                    new GroupTypeRoleInputDto(
                        "Leader",
                        IsLeader: true,
                        MaxCount: leaderMaxCount,
                        CanManageMembers: true
                    ),
                ]
            )
        );
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static async Task<GroupDetailResponse> CreateGroupAsync(
        ICommandDispatcher commands,
        Guid groupTypeId,
        string name,
        int? capacity
    )
    {
        var result = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: groupTypeId,
                Name: name,
                GroupCapacity: capacity,
                IsPublic: true
            )
        );
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
