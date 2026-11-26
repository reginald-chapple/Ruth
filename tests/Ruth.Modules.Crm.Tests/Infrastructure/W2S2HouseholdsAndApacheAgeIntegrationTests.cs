using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W2S2HouseholdsAndApacheAgeIntegrationTests(CrmPostgresContainerFixture fixture)
{
    [Fact]
    public async Task Households_KnownRelationships_PeerNetwork_SyncAndTraverseApacheAgeGraph()
    {
        var conn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (parentA, parentB, friend) = await SeedThreePeopleAsync(commands);
        var householdId = await VerifyHouseholdAndLocationsAsync(
            commands,
            queries,
            parentA,
            parentB
        );
        await VerifyRelationshipsAndPeerNetworkAsync(commands, queries, parentA, parentB, friend);
        await VerifyApacheAgeMultiHopGraphAsync(queries, parentA, parentB, friend, householdId);
    }

    private static async Task<(Guid ParentA, Guid ParentB, Guid Friend)> SeedThreePeopleAsync(
        ICommandDispatcher commands
    )
    {
        var a = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(
                    "Daniel",
                    "Reed",
                    Gender: Gender.Male,
                    MaritalStatus: MaritalStatus.Married
                )
            )
        )
            .Value
            .Id;
        var b = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(
                    "Sarah",
                    "Reed",
                    Gender: Gender.Female,
                    MaritalStatus: MaritalStatus.Married
                )
            )
        )
            .Value
            .Id;
        var friend = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Marcus", "Cole", Gender: Gender.Male)
            )
        )
            .Value
            .Id;
        return (a, b, friend);
    }

    private static async Task<Guid> VerifyHouseholdAndLocationsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid parentA,
        Guid parentB
    )
    {
        var branchId = Guid.CreateVersion7();
        var hhResult = await commands.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
            new CreateHouseholdCommand(
                "Reed Family",
                BranchId: branchId,
                Members:
                [
                    new HouseholdMemberInputDto(
                        parentA,
                        HouseholdRole.Adult,
                        HouseholdOrder: 0,
                        IsCombinedContributionMember: true,
                        IsPrimaryContact: true
                    ),
                    new HouseholdMemberInputDto(
                        parentB,
                        HouseholdRole.Adult,
                        HouseholdOrder: 0,
                        IsCombinedContributionMember: true
                    ),
                ]
            )
        );
        Assert.True(hhResult.IsSuccess);
        Assert.Equal("Daniel & Sarah Reed", hhResult.Value.Salutation);
        Assert.Equal(parentA, hhResult.Value.PrimaryContactPersonId);

        var refreshedA = (
            await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(parentA)
            )
        ).Value;
        Assert.Equal(hhResult.Value.Id, refreshedA.PrimaryHouseholdId);
        Assert.Equal(branchId, refreshedA.PrimaryBranchId);
        Assert.Equal(AgeClassification.Adult, refreshedA.AgeClassification);

        var loc1 = Guid.CreateVersion7();
        var loc2 = Guid.CreateVersion7();
        _ = await commands.SendAsync<SetHouseholdLocationCommand, HouseholdResponse>(
            new SetHouseholdLocationCommand(
                hhResult.Value.Id,
                loc1,
                HouseholdLocationType.Home,
                IsMailingLocation: true,
                IsMappedLocation: true,
                "100 First St"
            )
        );
        var afterLoc2 = (
            await commands.SendAsync<SetHouseholdLocationCommand, HouseholdResponse>(
                new SetHouseholdLocationCommand(
                    hhResult.Value.Id,
                    loc2,
                    HouseholdLocationType.Home,
                    IsMailingLocation: true,
                    IsMappedLocation: true,
                    "200 Second Ave"
                )
            )
        ).Value;
        Assert.Contains(afterLoc2.Locations, l =>
            l.LocationId == loc1 && l.LocationType == HouseholdLocationType.Previous
        );
        Assert.Contains(afterLoc2.Locations, l =>
            l.LocationId == loc2 && l.LocationType == HouseholdLocationType.Home
        );

        return hhResult.Value.Id;
    }

    private static async Task VerifyRelationshipsAndPeerNetworkAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid parentA,
        Guid parentB,
        Guid friend
    )
    {
        var relResult = await commands.SendAsync<
            AddKnownRelationshipCommand,
            KnownRelationshipResponse
        >(
            new AddKnownRelationshipCommand(
                parentB,
                friend,
                KnownRelationshipType.CanCheckIn,
                "Neighbor"
            )
        );
        Assert.True(relResult.IsSuccess);

        var friendRels = (
            await queries.QueryAsync<
                GetPersonKnownRelationshipsQuery,
                IReadOnlyList<KnownRelationshipResponse>
            >(new GetPersonKnownRelationshipsQuery(friend))
        ).Value;
        Assert.Contains(friendRels, r =>
            r.RelatedPersonId == parentB
            && r.RelationshipType == KnownRelationshipType.AllowCheckInBy
        );

        var peerResult = await commands.SendAsync<
            UpsertPeerNetworkEdgeCommand,
            PeerNetworkEdgeResponse
        >(
            new UpsertPeerNetworkEdgeCommand(
                parentA,
                friend,
                PeerNetworkRelationshipType.Following,
                RelationshipStrength.Casual,
                "Following",
                new DateOnly(2026, 6, 1)
            )
        );
        Assert.True(peerResult.IsSuccess);
        Assert.True(peerResult.Value.RelationshipScore >= 5.0m);
    }

    private static async Task VerifyApacheAgeMultiHopGraphAsync(
        IQueryDispatcher queries,
        Guid parentA,
        Guid parentB,
        Guid friend,
        Guid householdId
    )
    {
        var graphResult = await queries.QueryAsync<
            GetPersonRelationshipGraphQuery,
            PersonRelationshipGraphResponse
        >(new GetPersonRelationshipGraphQuery(parentA, MaxHops: 2));
        Assert.True(graphResult.IsSuccess);
        var graph = graphResult.Value;
        Assert.Contains(graph.Nodes, n => n.EntityId == parentA && n.HopDistance == 0);
        Assert.Contains(graph.Nodes, n =>
            n.EntityId == householdId
            && string.Equals(n.Label, "Household", StringComparison.Ordinal)
        );
        Assert.Contains(graph.Nodes, n => n.EntityId == parentB);
        Assert.Contains(graph.Nodes, n => n.EntityId == friend);
        Assert.NotEmpty(graph.Edges);
    }
}
