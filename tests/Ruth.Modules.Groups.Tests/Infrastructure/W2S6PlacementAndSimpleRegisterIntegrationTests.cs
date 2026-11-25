using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S6PlacementAndSimpleRegisterIntegrationTests(
    GroupsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task SimpleRegister_AndGroupPlacementBoard_MoveMembersCleanly()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Community Groups",
                InitialRoles: [new GroupTypeRoleInputDto("Member")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);

        var sourceGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(groupTypeResult.Value.Id, "Waitlist Group", IsPublic: true)
        );
        var destGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(groupTypeResult.Value.Id, "North Group", IsPublic: true)
        );
        Assert.True(sourceGroup.IsSuccess && destGroup.IsSuccess);

        var personId = Guid.CreateVersion7();
        var reg = await commands.SendAsync<SimpleRegisterForGroupCommand, GroupMemberResponse>(
            new SimpleRegisterForGroupCommand(sourceGroup.Value.Id, personId)
        );
        Assert.True(reg.IsSuccess);

        var board = await queries.QueryAsync<
            GetGroupPlacementBoardQuery,
            GroupPlacementBoardResponse
        >(new GetGroupPlacementBoardQuery([destGroup.Value.Id], sourceGroup.Value.Id));
        Assert.True(board.IsSuccess);
        _ = Assert.Single(board.Value.SourceMembers);

        var placed = await commands.SendAsync<
            PlaceGroupMembersCommand,
            GroupPlacementResultResponse
        >(
            new PlaceGroupMembersCommand(
                [new GroupPlacementAssignmentDto(personId, destGroup.Value.Id)],
                RemoveFromSourceGroupId: sourceGroup.Value.Id
            )
        );
        Assert.True(placed.IsSuccess);
        Assert.Equal(1, placed.Value.PlacedCount);
        Assert.Equal(0, placed.Value.FailedCount);

        var sourceAfter = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(sourceGroup.Value.Id));
        Assert.Empty(sourceAfter.Value);

        var destAfter = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(destGroup.Value.Id));
        var moved = Assert.Single(destAfter.Value);
        Assert.Equal(personId, moved.PersonId);
    }
}
