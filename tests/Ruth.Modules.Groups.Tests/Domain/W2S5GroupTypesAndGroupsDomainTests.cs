using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Domain;

public sealed class W2S5GroupTypesAndGroupsDomainTests
{
    private static readonly DateTimeOffset _now = new(2026, 5, 10, 14, 0, 0, TimeSpan.Zero);

    private static GroupTypeInput CreateTypeInput(
        string name = "Small Group",
        bool allowMultipleLocations = false,
        bool groupsRequireBranch = false,
        GroupCapacityRule capacityRule = GroupCapacityRule.Soft
    )
    {
        return new GroupTypeInput(
            name,
            "Community small groups",
            "Small Group",
            "Member",
            "people-team",
            0,
            ShowInGroupList: true,
            ShowInNavigation: true,
            allowMultipleLocations,
            IsMeetingStyleEnabled: true,
            groupsRequireBranch,
            EnableGroupHistory: true,
            TakesAttendance: true,
            CountsTowardHeadlineAttendance: false,
            SendAttendanceReminder: false,
            GroupAttendanceRequiresLocation: false,
            GroupAttendanceRequiresSchedule: false,
            AttendanceRule.None,
            capacityRule,
            GroupLocationPickerMode.Named | GroupLocationPickerMode.Point,
            EnableLocationSchedules: true,
            IsSchedulingEnabled: true,
            ScheduleConfirmationLogic.Ask,
            ScheduleReminderEmailOffsetDays: 2,
            PurposeValueId: null,
            InheritedGroupTypeId: null
        );
    }

    private static GroupInput CreateGroupInput(
        string name = "Downtown Group",
        GroupId? parentId = null,
        Guid? branchId = null,
        int? capacity = 12,
        bool isActive = true
    )
    {
        return new GroupInput(
            name,
            parentId,
            branchId,
            ScheduleId: null,
            Description: "Weekly community meetup",
            PhotoId: null,
            MeetingStyle.Hybrid,
            isActive,
            IsPublic: true,
            Order: 0,
            AllowGuests: true,
            capacity,
            StatusValueId: null,
            RequiredSignatureDocumentTemplateId: null,
            SchedulingMustMeetRequirements: false,
            ScheduleCoordinatorPersonId: null,
            GroupAdministratorPersonId: null,
            DisableScheduling: false,
            DisableScheduleToolboxAccess: false,
            RsvpReminderOffsetDays: 3
        );
    }

    [Fact]
    public void GroupType_Create_ValidatesRoles_UniqueNames_Counts_AndDefaultRoleGuard()
    {
        var typeResult = GroupType.Create(
            CreateTypeInput(),
            [
                new GroupTypeRoleInputDto("Leader", MinCount: 1, MaxCount: 2, IsLeader: true),
                new GroupTypeRoleInputDto("Member", MinCount: 0, MaxCount: 20),
            ]
        );
        Assert.True(typeResult.IsSuccess);
        var gt = typeResult.Value;
        Assert.Equal(2, gt.Roles.Count);
        Assert.True(gt.AllowsChildGroupType(gt.Id));

        var duplicate = gt.AddRole(new GroupTypeRoleInputDto("leader"));
        Assert.True(duplicate.IsFailure);
        Assert.Equal(GroupsErrors.DuplicateRoleName.Code, duplicate.PrimaryError.Code);

        var badCounts = gt.AddRole(new GroupTypeRoleInputDto("Host", MinCount: 5, MaxCount: 2));
        Assert.True(badCounts.IsFailure);
        Assert.Equal(GroupsErrors.InvalidRoleCounts.Code, badCounts.PrimaryError.Code);

        var removeDefault = gt.RemoveRole(gt.DefaultGroupRoleId!.Value);
        Assert.True(removeDefault.IsFailure);
        Assert.Equal(GroupsErrors.RoleInUse.Code, removeDefault.PrimaryError.Code);

        var memberRole = gt.Roles.First(static r =>
            string.Equals(r.Name, "Member", StringComparison.Ordinal)
        );
        var renamed = gt.UpdateRole(
            memberRole.Id,
            new GroupTypeRoleInputDto("Participant", MaxCount: 30)
        );
        Assert.True(renamed.IsSuccess);
        Assert.Equal("Participant", memberRole.Name);

        var removed = gt.RemoveRole(memberRole.Id);
        Assert.True(removed.IsSuccess);
        _ = Assert.Single(gt.Roles);
    }

    [Fact]
    public void Group_EnforcesBranchRequirement_Capacity_Archive_AndSingleLocationLimit()
    {
        var gt = GroupType
            .Create(CreateTypeInput(allowMultipleLocations: false, groupsRequireBranch: true))
            .Value;

        var missingBranch = Group.Create(gt, CreateGroupInput(branchId: null), _now);
        Assert.True(missingBranch.IsFailure);
        Assert.Equal(GroupsErrors.BranchRequired.Code, missingBranch.PrimaryError.Code);

        var branchId = Guid.CreateVersion7();
        var badCapacity = Group.Create(
            gt,
            CreateGroupInput(branchId: branchId, capacity: -1),
            _now
        );
        Assert.True(badCapacity.IsFailure);
        Assert.Equal(GroupsErrors.InvalidCapacity.Code, badCapacity.PrimaryError.Code);

        var group = Group.Create(gt, CreateGroupInput(branchId: branchId), _now).Value;
        var selfCycle = group.Update(
            gt,
            CreateGroupInput(parentId: group.Id, branchId: branchId),
            _now
        );
        Assert.True(selfCycle.IsFailure);
        Assert.Equal(GroupsErrors.ParentGroupCycle.Code, selfCycle.PrimaryError.Code);

        var loc1 = group.AttachOrUpdateLocation(
            gt,
            Guid.CreateVersion7(),
            "Main House",
            groupLocationTypeValueId: null,
            isMailingLocation: false,
            isMappedLocation: true,
            isOverflowLocation: false,
            groupMemberPersonId: null,
            0,
            new GroupGeoPointDto(39.7392, -104.9903),
            geoFenceRing: null,
            [Guid.CreateVersion7()]
        );
        Assert.True(loc1.IsSuccess);

        var loc2 = group.AttachOrUpdateLocation(
            gt,
            Guid.CreateVersion7(),
            "Second House",
            groupLocationTypeValueId: null,
            isMailingLocation: false,
            isMappedLocation: true,
            isOverflowLocation: false,
            groupMemberPersonId: null,
            1,
            geoPoint: null,
            geoFenceRing: null,
            scheduleIds: null
        );
        Assert.True(loc2.IsFailure);
        Assert.Equal(GroupsErrors.MultipleLocationsNotAllowed.Code, loc2.PrimaryError.Code);

        var archivedBy = Guid.CreateVersion7();
        _ = group.SetArchiveState(archive: true, archivedBy, _now);
        Assert.True(group.IsArchived);
        Assert.False(group.IsActive);
        Assert.Equal(archivedBy, group.ArchivedByPersonId);

        var snapshot = GroupHistorical.CreateSnapshot(group, gt.Name, _now);
        Assert.True(snapshot.CurrentRowIndicator);
        Assert.Equal(GroupHistorical.MaxExpireDateTimeUtc, snapshot.ExpireDateTimeUtc);
        snapshot.ExpireAt(_now.AddDays(1));
        Assert.False(snapshot.CurrentRowIndicator);
    }

    [Fact]
    public void GeoHelper_AndDemographicValue_ValidateAndParseTypedProjections()
    {
        var badPoint = GroupsGeoHelper.CreatePoint(new GroupGeoPointDto(95.0, -104.0));
        Assert.True(badPoint.IsFailure);

        var goodPoint = GroupsGeoHelper.CreatePoint(new GroupGeoPointDto(39.7392, -104.9903));
        Assert.True(goodPoint.IsSuccess);
        Assert.Equal(39.7392, GroupsGeoHelper.ToPointDto(goodPoint.Value)!.Latitude);

        var badPoly = GroupsGeoHelper.CreatePolygon([
            new GroupGeoPointDto(0, 0),
            new GroupGeoPointDto(1, 1),
        ]);
        Assert.True(badPoly.IsFailure);

        var goodPoly = GroupsGeoHelper.CreatePolygon([
            new GroupGeoPointDto(39.70, -105.05),
            new GroupGeoPointDto(39.70, -104.90),
            new GroupGeoPointDto(39.80, -104.90),
            new GroupGeoPointDto(39.80, -105.05),
        ]);
        Assert.True(goodPoly.IsSuccess);
        Assert.NotNull(GroupsGeoHelper.ToRingDto(goodPoly.Value));

        var demoType = GroupDemographicType
            .Create(GroupTypeId.Create(), "Average Age", "avg-age")
            .Value;
        var numVal = GroupDemographicValue
            .Create(
                GroupId.Create(),
                demoType.Id,
                "34.50",
                relatedEntityTypeKey: null,
                relatedEntityId: null,
                _now
            )
            .Value;
        Assert.Equal(34.50m, numVal.ValueAsNumeric);

        var boolVal = GroupDemographicValue
            .Create(
                GroupId.Create(),
                demoType.Id,
                "true",
                relatedEntityTypeKey: null,
                relatedEntityId: null,
                _now
            )
            .Value;
        Assert.True(boolVal.ValueAsBoolean);

        var guidTarget = Guid.CreateVersion7();
        var guidVal = GroupDemographicValue
            .Create(
                GroupId.Create(),
                demoType.Id,
                guidTarget.ToString("D"),
                relatedEntityTypeKey: null,
                relatedEntityId: null,
                _now
            )
            .Value;
        Assert.Equal(guidTarget, guidVal.ValueAsGuid);
    }
}
