using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Ruth.Modules.Groups.Domain.Sync;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Domain;

public sealed class W2S6MembersRequirementsSyncDomainTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GroupMembershipPolicy_EnforcesRoleMaxCount_HardAndSoftCapacity_WithGuests()
    {
        var softType = GroupType
            .Create(
                new GroupTypeInput(
                    "Soft Group",
                    Description: null,
                    "Group",
                    "Member",
                    IconCssClass: null,
                    0,
                    ShowInGroupList: true,
                    ShowInNavigation: true,
                    AllowMultipleLocations: false,
                    IsMeetingStyleEnabled: false,
                    GroupsRequireBranch: false,
                    EnableGroupHistory: true,
                    TakesAttendance: true,
                    CountsTowardHeadlineAttendance: false,
                    SendAttendanceReminder: false,
                    GroupAttendanceRequiresLocation: false,
                    GroupAttendanceRequiresSchedule: false,
                    AttendanceRule.None,
                    GroupCapacityRule.Soft,
                    GroupLocationPickerMode.Named,
                    EnableLocationSchedules: false,
                    IsSchedulingEnabled: false,
                    ScheduleConfirmationLogic.Ask,
                    ScheduleReminderEmailOffsetDays: null,
                    PurposeValueId: null,
                    InheritedGroupTypeId: null
                ),
                [new GroupTypeRoleInputDto("Member", MaxCount: 2)]
            )
            .Value;
        var role = softType.Roles.First();
        var group = Group
            .Create(
                softType,
                new GroupInput(
                    "Study",
                    ParentGroupId: null,
                    BranchId: null,
                    ScheduleId: null,
                    Description: null,
                    PhotoId: null,
                    MeetingStyle: null,
                    IsActive: true,
                    IsPublic: true,
                    0,
                    AllowGuests: true,
                    GroupCapacity: 3,
                    StatusValueId: null,
                    RequiredSignatureDocumentTemplateId: null,
                    SchedulingMustMeetRequirements: false,
                    ScheduleCoordinatorPersonId: null,
                    GroupAdministratorPersonId: null,
                    DisableScheduling: false,
                    DisableScheduleToolboxAccess: false,
                    RsvpReminderOffsetDays: null
                ),
                _now
            )
            .Value;

        var roleMaxExceeded = GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            softType,
            role,
            GroupMemberStatus.Active,
            existingActiveInRoleExcludingSelf: 2,
            existingActiveUnitsInGroupExcludingSelf: 2,
            targetGuestCount: 0
        );
        Assert.True(roleMaxExceeded.IsFailure);
        Assert.Equal(GroupsErrors.RoleMaxCountExceeded.Code, roleMaxExceeded.PrimaryError.Code);

        var softWarning = GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            softType,
            role,
            GroupMemberStatus.Active,
            existingActiveInRoleExcludingSelf: 1,
            existingActiveUnitsInGroupExcludingSelf: 2,
            targetGuestCount: 2
        );
        Assert.True(softWarning.IsSuccess);
        Assert.NotNull(softWarning.Value);

        var pendingBypass = GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            softType,
            role,
            GroupMemberStatus.Pending,
            existingActiveInRoleExcludingSelf: 2,
            existingActiveUnitsInGroupExcludingSelf: 10,
            targetGuestCount: 5
        );
        Assert.True(pendingBypass.IsSuccess);
        Assert.Null(pendingBypass.Value);

        var hardType = GroupType
            .Create(
                new GroupTypeInput(
                    "Hard Group",
                    Description: null,
                    "Group",
                    "Member",
                    IconCssClass: null,
                    0,
                    ShowInGroupList: true,
                    ShowInNavigation: true,
                    AllowMultipleLocations: false,
                    IsMeetingStyleEnabled: false,
                    GroupsRequireBranch: false,
                    EnableGroupHistory: false,
                    TakesAttendance: false,
                    CountsTowardHeadlineAttendance: false,
                    SendAttendanceReminder: false,
                    GroupAttendanceRequiresLocation: false,
                    GroupAttendanceRequiresSchedule: false,
                    AttendanceRule.None,
                    GroupCapacityRule.Hard,
                    GroupLocationPickerMode.Named,
                    EnableLocationSchedules: false,
                    IsSchedulingEnabled: false,
                    ScheduleConfirmationLogic.Ask,
                    ScheduleReminderEmailOffsetDays: null,
                    PurposeValueId: null,
                    InheritedGroupTypeId: null
                ),
                [new GroupTypeRoleInputDto("Member")]
            )
            .Value;
        var hardBlock = GroupMembershipPolicy.EvaluateCapacityAndRoleLimits(
            group,
            hardType,
            hardType.Roles.First(),
            GroupMemberStatus.Active,
            existingActiveInRoleExcludingSelf: 1,
            existingActiveUnitsInGroupExcludingSelf: 3,
            targetGuestCount: 0
        );
        Assert.True(hardBlock.IsFailure);
        Assert.Equal(GroupsErrors.GroupHardCapacityExceeded.Code, hardBlock.PrimaryError.Code);
    }

    [Fact]
    public void GroupRequirement_ScopeXor_AppliesTo_Expiration_DueDate_AndOverrideGuards()
    {
        var reqType = GroupRequirementType
            .Create(
                "Background Check",
                RequirementCheckType.BackgroundCheckClear,
                description: null,
                summary: null,
                "standard-volunteer",
                ruleValue: null,
                canExpire: true,
                expireInDays: 365,
                warningDaysBeforeExpiration: 30,
                "Clear",
                "Missing Check",
                "Expiring Soon",
                checkboxLabel: null,
                iconCssClass: null,
                RequirementDueDateType.DaysAfterJoining,
                dueDateOffsetInDays: 14,
                categoryId: null
            )
            .Value;

        Assert.Equal("Clear", reqType.ResolveLabel(MeetsGroupRequirement.Meets));
        Assert.Equal("Expiring Soon", reqType.ResolveLabel(MeetsGroupRequirement.MeetsWithWarning));
        Assert.Equal("Missing Check", reqType.ResolveLabel(MeetsGroupRequirement.NotMet));

        var bothScopes = GroupRequirement.Create(
            reqType.Id,
            GroupId.Create(),
            GroupTypeId.Create(),
            groupRoleId: null,
            mustMeetToAdd: true,
            allowLeadersToOverride: false,
            RequirementAgeClassification.All,
            dueDateStaticDate: null,
            dueDateAttributeKey: null
        );
        Assert.True(bothScopes.IsFailure);
        Assert.Equal(GroupsErrors.RequirementInvalidScope.Code, bothScopes.PrimaryError.Code);

        var adultRole = GroupTypeRoleId.Create();
        var req = GroupRequirement
            .Create(
                reqType.Id,
                GroupId.Create(),
                groupTypeId: null,
                adultRole,
                mustMeetToAdd: true,
                allowLeadersToOverride: false,
                RequirementAgeClassification.Adults,
                dueDateStaticDate: null,
                dueDateAttributeKey: null
            )
            .Value;
        Assert.True(req.AppliesTo(adultRole, isAdult: true));
        Assert.False(req.AppliesTo(adultRole, isAdult: false));
        Assert.False(req.AppliesTo(GroupTypeRoleId.Create(), isAdult: true));

        Assert.Equal(
            MeetsGroupRequirement.NotMet,
            GroupRequirementPolicy.EvaluateTimedEvidence(reqType, completedOnUtc: null, _now)
        );
        Assert.Equal(
            MeetsGroupRequirement.Meets,
            GroupRequirementPolicy.EvaluateTimedEvidence(reqType, _now.AddDays(-100), _now)
        );
        Assert.Equal(
            MeetsGroupRequirement.MeetsWithWarning,
            GroupRequirementPolicy.EvaluateTimedEvidence(reqType, _now.AddDays(-350), _now)
        );
        Assert.Equal(
            MeetsGroupRequirement.NotMet,
            GroupRequirementPolicy.EvaluateTimedEvidence(reqType, _now.AddDays(-400), _now)
        );

        var due = GroupRequirementPolicy.ComputeDueDate(
            reqType,
            req,
            _now,
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            _now
        );
        Assert.Equal(new DateOnly(2026, 6, 15), due);

        var memberReq = GroupMemberRequirement.Create(GroupMemberId.Create(), req.Id, due);
        var forbiddenOverride = memberReq.SetManualOrOverride(
            isOverride: true,
            allowLeaderOverride: false,
            isMet: true,
            Guid.CreateVersion7(),
            "tried",
            _now
        );
        Assert.True(forbiddenOverride.IsFailure);
        Assert.Equal(
            GroupsErrors.RequirementOverrideNotAllowed.Code,
            forbiddenOverride.PrimaryError.Code
        );

        var manualOk = memberReq.SetManualOrOverride(
            isOverride: false,
            allowLeaderOverride: false,
            isMet: true,
            Guid.CreateVersion7(),
            "completed",
            _now
        );
        Assert.True(manualOk.IsSuccess);
        Assert.Equal(MeetsGroupRequirement.Meets, memberReq.State);
    }

    [Fact]
    public void GroupSyncRule_ValidatesSourceAndScheduleDueWindow()
    {
        var invalid = GroupSyncRule.Create(
            GroupId.Create(),
            GroupTypeRoleId.Create(),
            GroupSyncSourceType.EntitySet,
            sourceReferenceId: null,
            sourceFilterValue: null,
            scheduleIntervalMinutes: 30,
            addMissingMembers: true,
            removeExtraMembers: true,
            GroupMemberStatus.Active
        );
        Assert.True(invalid.IsFailure);
        Assert.Equal(GroupsErrors.InvalidSyncSource.Code, invalid.PrimaryError.Code);

        var valid = GroupSyncRule
            .Create(
                GroupId.Create(),
                GroupTypeRoleId.Create(),
                GroupSyncSourceType.CrmConnectionStatus,
                sourceReferenceId: null,
                sourceFilterValue: "Member",
                scheduleIntervalMinutes: 30,
                addMissingMembers: true,
                removeExtraMembers: true,
                GroupMemberStatus.Active
            )
            .Value;
        Assert.True(valid.IsDue(_now));
        valid.RecordExecution(_now, addedCount: 3, removedCount: 1);
        Assert.False(valid.IsDue(_now.AddMinutes(15)));
        Assert.True(valid.IsDue(_now.AddMinutes(30)));
    }
}
