using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Infrastructure;

[Collection(GroupsPostgresFixtures.Name)]
public sealed class W2S5GroupHierarchyAndPostGisFinderIntegrationTests(
    GroupsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task GroupHierarchy_HistoryScdType2_AndAllowedChildren_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (parentType, childType) = await CreateParentAndChildGroupTypesAsync(commands);

        var parentGroupResult = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: parentType.Id,
                Name: "Metro Area Network",
                Description: "Regional parent group"
            )
        );
        Assert.True(parentGroupResult.IsSuccess);
        var parentGroup = parentGroupResult.Value;

        var childGroupResult = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: childType.Id,
                Name: "Capitol Hill Small Group",
                ParentGroupId: parentGroup.Id,
                GroupCapacity: 12
            )
        );
        Assert.True(childGroupResult.IsSuccess);
        var childGroup = childGroupResult.Value;

        // Disallowed child type (parentType cannot be nested under childType)
        var disallowedChild = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: parentType.Id,
                Name: "Illegal Reverse Child",
                ParentGroupId: childGroup.Id
            )
        );
        Assert.True(disallowedChild.IsFailure);

        // Circular parent rejection (setting parentGroup's parent to its child)
        var circularUpdate = await commands.SendAsync<UpdateGroupCommand, GroupDetailResponse>(
            new UpdateGroupCommand(
                GroupId: parentGroup.Id,
                Name: parentGroup.Name,
                ParentGroupId: childGroup.Id
            )
        );
        Assert.True(circularUpdate.IsFailure);

        await VerifyHistoryRotationAndTreeAsync(commands, queries, parentGroup.Id, childGroup.Id);
    }

    [Fact]
    public async Task PostGisGroupFinder_SupportsRadiusOrdering_Geofence_Polygon_AndFilters()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await GroupsTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var groupType = await CreateSmallGroupTypeAsync(commands);
        var demoTypeResult = await commands.SendAsync<
            CreateGroupDemographicTypeCommand,
            GroupDemographicTypeResponse
        >(
            new CreateGroupDemographicTypeCommand(
                GroupTypeId: groupType.Id,
                Name: "Stage of Life",
                ComponentKey: "stage_of_life"
            )
        );
        Assert.True(demoTypeResult.IsSuccess);

        var (denverGroup, boulderGroup) = await SeedSpatialGroupsAsync(
            commands,
            groupType.Id,
            demoTypeResult.Value.Id
        );

        await VerifyRadiusAndGeofenceSearchAsync(queries, groupType.Id, denverGroup, boulderGroup);
        await VerifyPolygonAndAttributeFilterSearchAsync(
            queries,
            groupType.Id,
            denverGroup,
            boulderGroup
        );
    }

    private static async Task<(
        GroupTypeDetailResponse ParentType,
        GroupTypeDetailResponse ChildType
    )> CreateParentAndChildGroupTypesAsync(ICommandDispatcher commands)
    {
        var childTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Life Group",
                GroupTerm: "Life Group",
                EnableGroupHistory: true,
                InitialRoles:
                [
                    new GroupTypeRoleInputDto("Leader", IsLeader: true, CanManageMembers: true),
                    new GroupTypeRoleInputDto("Member"),
                ]
            )
        );
        Assert.True(childTypeResult.IsSuccess);

        var parentTypeResult = await commands.SendAsync<
            CreateGroupTypeCommand,
            GroupTypeDetailResponse
        >(
            new CreateGroupTypeCommand(
                Name: "Group Network",
                GroupTerm: "Network",
                EnableGroupHistory: true,
                AllowedChildGroupTypeIds: [childTypeResult.Value.Id]
            )
        );
        Assert.True(parentTypeResult.IsSuccess);

        return (parentTypeResult.Value, childTypeResult.Value);
    }

    private static async Task VerifyHistoryRotationAndTreeAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid parentGroupId,
        Guid childGroupId
    )
    {
        var updateResult = await commands.SendAsync<UpdateGroupCommand, GroupDetailResponse>(
            new UpdateGroupCommand(
                GroupId: childGroupId,
                Name: "Capitol Hill Evening Group",
                ParentGroupId: parentGroupId,
                Description: "Updated description",
                GroupCapacity: 14
            )
        );
        Assert.True(updateResult.IsSuccess);

        var archiveResult = await commands.SendAsync<
            SetGroupArchiveStateCommand,
            GroupDetailResponse
        >(new SetGroupArchiveStateCommand(childGroupId, IsArchived: true));
        Assert.True(archiveResult.IsSuccess);

        var historyResult = await queries.QueryAsync<
            GetGroupHistoryQuery,
            IReadOnlyList<GroupHistoricalResponse>
        >(new GetGroupHistoryQuery(childGroupId));
        Assert.True(historyResult.IsSuccess);
        Assert.True(historyResult.Value.Count >= 3);
        _ = Assert.Single(historyResult.Value, static h => h.CurrentRowIndicator);
        Assert.True(historyResult.Value.Single(static h => h.CurrentRowIndicator).IsArchived);

        var unarchiveResult = await commands.SendAsync<
            SetGroupArchiveStateCommand,
            GroupDetailResponse
        >(new SetGroupArchiveStateCommand(childGroupId, IsArchived: false));
        Assert.True(unarchiveResult.IsSuccess);
        Assert.False(unarchiveResult.Value.IsArchived);
        Assert.False(unarchiveResult.Value.IsActive);

        // Reactivate child group so it appears in the active group tree
        var reactivateResult = await commands.SendAsync<UpdateGroupCommand, GroupDetailResponse>(
            new UpdateGroupCommand(
                GroupId: childGroupId,
                Name: "Capitol Hill Evening Group",
                ParentGroupId: parentGroupId,
                Description: "Updated description",
                IsActive: true,
                GroupCapacity: 14
            )
        );
        Assert.True(reactivateResult.IsSuccess);

        var treeResult = await queries.QueryAsync<
            GetGroupTreeQuery,
            IReadOnlyList<GroupTreeNodeResponse>
        >(new GetGroupTreeQuery());
        Assert.True(treeResult.IsSuccess);
        var rootNode = Assert.Single(treeResult.Value, n => n.Id == parentGroupId);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal(childGroupId, childNode.Id);
        Assert.Equal("Capitol Hill Evening Group", childNode.Name);
    }

    private static async Task<GroupTypeDetailResponse> CreateSmallGroupTypeAsync(
        ICommandDispatcher commands
    )
    {
        var result = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand(
                Name: "Small Groups Finder Test",
                GroupTerm: "Small Group",
                AllowMultipleLocations: true,
                IsMeetingStyleEnabled: true,
                GroupCapacityRule: GroupCapacityRule.Hard,
                InitialRoles: [new GroupTypeRoleInputDto("Member")]
            )
        );
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static async Task<(Guid DenverGroupId, Guid BoulderGroupId)> SeedSpatialGroupsAsync(
        ICommandDispatcher commands,
        Guid groupTypeId,
        Guid demographicTypeId
    )
    {
        var denverGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: groupTypeId,
                Name: "Downtown Denver Study",
                MeetingStyle: MeetingStyle.InPerson,
                GroupCapacity: 10,
                Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["childcare"] = "true",
                    ["topic"] = "Romans",
                }
            )
        );
        Assert.True(denverGroup.IsSuccess);

        var denverLoc = await commands.SendAsync<AttachGroupLocationCommand, GroupDetailResponse>(
            new AttachGroupLocationCommand(
                GroupId: denverGroup.Value.Id,
                LocationId: Guid.CreateVersion7(),
                LocationName: "Denver Civic Center",
                GeoPoint: new GroupGeoPointDto(39.7392, -104.9903),
                GeoFenceRing:
                [
                    new GroupGeoPointDto(39.70, -105.05),
                    new GroupGeoPointDto(39.70, -104.92),
                    new GroupGeoPointDto(39.78, -104.92),
                    new GroupGeoPointDto(39.78, -105.05),
                    new GroupGeoPointDto(39.70, -105.05),
                ]
            )
        );
        Assert.True(denverLoc.IsSuccess);

        var demoVal = await commands.SendAsync<
            SetGroupDemographicValueCommand,
            GroupDemographicValueResponse
        >(
            new SetGroupDemographicValueCommand(
                denverGroup.Value.Id,
                demographicTypeId,
                "Young Adults"
            )
        );
        Assert.True(demoVal.IsSuccess);

        var boulderGroup = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(
                GroupTypeId: groupTypeId,
                Name: "Boulder Creek Fellowship",
                MeetingStyle: MeetingStyle.Hybrid,
                GroupCapacity: 1,
                Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["childcare"] = "false",
                    ["topic"] = "Psalms",
                }
            )
        );
        Assert.True(boulderGroup.IsSuccess);

        var boulderLoc = await commands.SendAsync<AttachGroupLocationCommand, GroupDetailResponse>(
            new AttachGroupLocationCommand(
                GroupId: boulderGroup.Value.Id,
                LocationId: Guid.CreateVersion7(),
                LocationName: "Boulder Pearl St",
                GeoPoint: new GroupGeoPointDto(40.0150, -105.2705)
            )
        );
        Assert.True(boulderLoc.IsSuccess);

        // Fill Boulder group to capacity = 1 so HasOpenCapacityOnly filters it out
        var fillMember = await commands.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(boulderGroup.Value.Id, Guid.CreateVersion7())
        );
        Assert.True(fillMember.IsSuccess);

        return (denverGroup.Value.Id, boulderGroup.Value.Id);
    }

    private static async Task VerifyRadiusAndGeofenceSearchAsync(
        IQueryDispatcher queries,
        Guid groupTypeId,
        Guid denverGroupId,
        Guid boulderGroupId
    )
    {
        // 10 km around Denver matches Denver group via radius AND geofence, excludes Boulder (~40 km away)
        var nearDenver = await queries.QueryAsync<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>
        >(
            new FindGroupsQuery(
                GroupTypeId: groupTypeId,
                Latitude: 39.7400,
                Longitude: -104.9900,
                RadiusMeters: 10_000
            )
        );
        Assert.True(nearDenver.IsSuccess);
        var match = Assert.Single(nearDenver.Value);
        Assert.Equal(denverGroupId, match.GroupId);
        Assert.True(match.MatchedByGeofence);
        var distanceMeters = Assert.NotNull(match.DistanceMeters);
        Assert.True(distanceMeters < 500);

        // 80 km around Denver matches both, ordered closest first (Denver then Boulder)
        var wideSearch = await queries.QueryAsync<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>
        >(
            new FindGroupsQuery(
                GroupTypeId: groupTypeId,
                Latitude: 39.7400,
                Longitude: -104.9900,
                RadiusMeters: 80_000
            )
        );
        Assert.True(wideSearch.IsSuccess);
        Assert.Equal(2, wideSearch.Value.Count);
        Assert.Equal(denverGroupId, wideSearch.Value[0].GroupId);
        Assert.Equal(boulderGroupId, wideSearch.Value[1].GroupId);
        Assert.True(wideSearch.Value[0].DistanceMeters < wideSearch.Value[1].DistanceMeters);
    }

    private static async Task VerifyPolygonAndAttributeFilterSearchAsync(
        IQueryDispatcher queries,
        Guid groupTypeId,
        Guid denverGroupId,
        Guid boulderGroupId
    )
    {
        var boulderPolygon = await queries.QueryAsync<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>
        >(
            new FindGroupsQuery(
                GroupTypeId: groupTypeId,
                PolygonRing:
                [
                    new GroupGeoPointDto(39.98, -105.32),
                    new GroupGeoPointDto(39.98, -105.20),
                    new GroupGeoPointDto(40.06, -105.20),
                    new GroupGeoPointDto(40.06, -105.32),
                    new GroupGeoPointDto(39.98, -105.32),
                ]
            )
        );
        Assert.True(boulderPolygon.IsSuccess);
        var polyMatch = Assert.Single(boulderPolygon.Value);
        Assert.Equal(boulderGroupId, polyMatch.GroupId);

        var filtered = await queries.QueryAsync<
            FindGroupsQuery,
            IReadOnlyList<GroupFinderItemResponse>
        >(
            new FindGroupsQuery(
                GroupTypeId: groupTypeId,
                HasOpenCapacityOnly: true,
                AttributeFilters: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["childcare"] = "true",
                },
                DemographicFilters: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Stage of Life"] = "Young Adults",
                }
            )
        );
        Assert.True(filtered.IsSuccess);
        var filterMatch = Assert.Single(filtered.Value);
        Assert.Equal(denverGroupId, filterMatch.GroupId);
        Assert.True(filterMatch.HasOpenCapacity);
    }
}
