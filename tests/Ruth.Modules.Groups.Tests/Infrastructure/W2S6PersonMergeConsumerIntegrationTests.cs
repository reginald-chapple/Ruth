using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S6PersonMergeConsumerIntegrationTests(GroupsPostgresContainerFixture postgres)
{
    [Fact]
    public async Task GroupsPersonMergedConsumer_RepointsAndDeduplicatesMembershipsIdempotently()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var survivingPersonId = Guid.CreateVersion7();
        var retiredPersonId = Guid.CreateVersion7();
        var (sharedGroupId, retiredOnlyGroupId) = await SeedGroupsAndMembersAsync(
            commands,
            survivingPersonId,
            retiredPersonId
        );

        var consumer = scope
            .ServiceProvider.GetServices<IIntegrationEventConsumer<PersonMergedIntegrationEvent>>()
            .Single(static c =>
                c.GetType()
                    .FullName?.Contains("GroupsPersonMergedConsumer", StringComparison.Ordinal)
                == true
            );

        var mergeEvent = new PersonMergedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            SurvivingPersonId: survivingPersonId,
            RetiredPersonId: retiredPersonId,
            LegacyAliasGuids: [],
            HadMultipleDistinctEmails: false
        );

        await consumer.ConsumeAsync(mergeEvent);
        await consumer.ConsumeAsync(mergeEvent);

        await AssertMergeResultsAsync(
            queries,
            sharedGroupId,
            retiredOnlyGroupId,
            survivingPersonId,
            retiredPersonId
        );
    }

    private static async Task<(
        Guid SharedGroupId,
        Guid RetiredOnlyGroupId
    )> SeedGroupsAndMembersAsync(
        ICommandDispatcher commands,
        Guid survivingPersonId,
        Guid retiredPersonId
    )
    {
        var groupTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Merge Test Groups",
                InitialRoles: [new GroupTypeRoleInputDto("Member")]
            )
        );
        Assert.True(groupTypeResult.IsSuccess);
        var groupTypeId = groupTypeResult.Value.Id;

        var sharedGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(groupTypeId, "Shared Group")
        );
        var retiredOnlyGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(groupTypeId, "Retired Only Group")
        );
        Assert.True(sharedGroup.IsSuccess && retiredOnlyGroup.IsSuccess);

        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(sharedGroup.Value.Id, survivingPersonId)
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(sharedGroup.Value.Id, retiredPersonId)
                )
            ).IsSuccess
        );
        Assert.True(
            (
                await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
                    new AddGroupMemberCommand(retiredOnlyGroup.Value.Id, retiredPersonId)
                )
            ).IsSuccess
        );

        return (sharedGroup.Value.Id, retiredOnlyGroup.Value.Id);
    }

    private static async Task AssertMergeResultsAsync(
        IQueryDispatcher queries,
        Guid sharedGroupId,
        Guid retiredOnlyGroupId,
        Guid survivingPersonId,
        Guid retiredPersonId
    )
    {
        var retiredMemberships = await queries.QueryAsync<
            GetPersonGroupMembershipsQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetPersonGroupMembershipsQuery(retiredPersonId, ActiveOnly: false));
        Assert.Empty(retiredMemberships.Value);

        var repointed = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(retiredOnlyGroupId));
        _ = Assert.Single(repointed.Value, m => m.PersonId == survivingPersonId);

        var deduplicated = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(sharedGroupId));
        _ = Assert.Single(deduplicated.Value, m => m.PersonId == survivingPersonId);
    }
}
