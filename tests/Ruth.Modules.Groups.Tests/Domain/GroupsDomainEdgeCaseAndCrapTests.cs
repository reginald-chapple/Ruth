using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Domain.Requirements;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Domain;

public sealed class GroupsDomainEdgeCaseAndCrapTests
{
    private static readonly DateTimeOffset _now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TextPolicy_RejectsControlChars_AndGroupTypeUpdate_ValidatesDefaultRole()
    {
        Assert.True(GroupsTextPolicy.NormalizeRequired("Bad\u0007Name").IsFailure);
        Assert.True(
            GroupsTextPolicy
                .NormalizeOptional("Line1\nLine2", maxLength: 50, allowNewLines: false)
                .IsFailure
        );
        Assert.True(
            GroupsTextPolicy
                .NormalizeOptional("Line1\nLine2", maxLength: 50, allowNewLines: true)
                .IsSuccess
        );

        var input = BuildGroupTypeInput("Serving");
        var gt = GroupType
            .Create(
                input,
                [new GroupTypeRoleInputDto("Leader"), new GroupTypeRoleInputDto("Member")]
            )
            .Value;

        var missingRole = gt.Update(input, GroupTypeRoleId.Create(), allowedChildIds: null);
        Assert.True(missingRole.IsFailure);
        Assert.Equal(GroupsErrors.GroupTypeRoleNotFound.Code, missingRole.PrimaryError.Code);

        var memberRole = gt.Roles.Last();
        var okUpdate = gt.Update(
            BuildGroupTypeInput("Serving Updated"),
            memberRole.Id,
            [gt.Id, GroupTypeId.Create()]
        );
        Assert.True(okUpdate.IsSuccess);
        Assert.Equal("Serving Updated", gt.Name);
        Assert.Equal(memberRole.Id, gt.DefaultGroupRoleId);
        Assert.Equal(2, gt.AllowedChildren.Count);
    }

    [Fact]
    public void PersonMergeRepointing_CoversGroup_MemberHistory_Requirements_AndAttendance()
    {
        var retired = Guid.CreateVersion7();
        var surviving = Guid.CreateVersion7();
        var gt = GroupType.Create(BuildGroupTypeInput("Small Group")).Value;
        var defaultRole = gt.Roles.First();
        var group = Group
            .Create(
                gt,
                BuildGroupInput(
                    "Repoint Test",
                    coordinatorId: retired,
                    administratorId: retired,
                    allowGuests: true
                ),
                _now
            )
            .Value;
        var loc = group
            .AttachOrUpdateLocation(
                gt,
                Guid.CreateVersion7(),
                "Host Home",
                groupLocationTypeValueId: null,
                isMailingLocation: false,
                isMappedLocation: true,
                isOverflowLocation: false,
                groupMemberPersonId: retired,
                order: 0,
                geoPoint: null,
                geoFenceRing: null,
                scheduleIds: null
            )
            .Value;
        _ = group.SetArchiveState(archive: true, archivedByPersonId: retired, _now);

        group.RepointPersonReferences(retired, surviving);
        Assert.Equal(surviving, group.ScheduleCoordinatorPersonId);
        Assert.Equal(surviving, group.GroupAdministratorPersonId);
        Assert.Equal(surviving, group.ArchivedByPersonId);
        Assert.Equal(surviving, loc.GroupMemberPersonId);
        Assert.True(group.RemoveLocation(loc.Id).IsSuccess);
        Assert.True(group.RemoveLocation(GroupLocationId.Create()).IsFailure);

        var member = GroupMember
            .Create(
                group,
                retired,
                defaultRole.Id,
                GroupMemberStatus.Active,
                note: "Joined",
                guestCount: 2,
                groupOrder: 1,
                scheduleTemplateId: null,
                scheduleStartDate: null,
                scheduleReminderEmailOffsetDays: null,
                GroupCommunicationPreference.Email,
                _now
            )
            .Value;
        Assert.Equal(
            3,
            GroupMembershipPolicy.ComputeActiveOccupancyUnits([member], allowGuests: true)
        );
        Assert.Equal(
            1,
            GroupMembershipPolicy.ComputeActiveOccupancyUnits([member], allowGuests: false)
        );
        Assert.Equal(
            0,
            GroupMembershipPolicy.ComputeActiveOccupancyUnits(
                [member],
                allowGuests: true,
                excludeMemberId: member.Id
            )
        );

        VerifyMemberUpdatesAndHistoryRepoint(member, defaultRole, retired, surviving);
        VerifyAttendanceRescheduleAndRepoint(group.Id, gt.Id, retired, surviving);
    }

    [Fact]
    public void RequirementPolicy_AttributeDueDate_AndAutomatedEvaluationStates_WorkCleanly()
    {
        var reqType = GroupRequirementType
            .Create(
                "Training",
                RequirementCheckType.Manual,
                description: null,
                summary: null,
                ruleKey: null,
                ruleValue: null,
                canExpire: false,
                expireInDays: null,
                warningDaysBeforeExpiration: null,
                positiveLabel: null,
                negativeLabel: null,
                warningLabel: null,
                checkboxLabel: null,
                iconCssClass: null,
                RequirementDueDateType.GroupAttribute,
                dueDateOffsetInDays: 7,
                categoryId: null
            )
            .Value;
        var req = GroupRequirement
            .Create(
                reqType.Id,
                GroupId.Create(),
                groupTypeId: null,
                groupRoleId: null,
                mustMeetToAdd: false,
                allowLeadersToOverride: true,
                RequirementAgeClassification.All,
                dueDateStaticDate: null,
                dueDateAttributeKey: "launch_date"
            )
            .Value;

        var attrs = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["launch_date"] = "2026-08-01",
        };
        Assert.Equal(
            new DateOnly(2026, 8, 8),
            GroupRequirementPolicy.ComputeDueDate(reqType, req, _now, attrs, _now)
        );
        Assert.Null(
            GroupRequirementPolicy.ComputeDueDate(
                reqType,
                req,
                _now,
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["launch_date"] = "not-a-date",
                },
                _now
            )
        );

        var actor = Guid.CreateVersion7();
        var survivor = Guid.CreateVersion7();
        var memberReq = GroupMemberRequirement.Create(
            GroupMemberId.Create(),
            req.Id,
            dueDate: null
        );
        memberReq.ApplyAutomatedEvaluation(
            MeetsGroupRequirement.MeetsWithWarning,
            new DateOnly(2026, 8, 8),
            _now
        );
        Assert.Equal(MeetsGroupRequirement.MeetsWithWarning, memberReq.State);
        _ = Assert.NotNull(memberReq.RequirementWarningOnUtc);

        _ = memberReq.SetManualOrOverride(
            isOverride: false,
            allowLeaderOverride: true,
            isMet: true,
            actor,
            note: "Done",
            _now
        );
        _ = memberReq.SetManualOrOverride(
            isOverride: true,
            allowLeaderOverride: true,
            isMet: true,
            actor,
            note: "Approved",
            _now
        );
        memberReq.ApplyAutomatedEvaluation(
            MeetsGroupRequirement.NotMet,
            new DateOnly(2026, 8, 8),
            _now.AddDays(1)
        );
        Assert.Equal(MeetsGroupRequirement.Meets, memberReq.State);

        memberReq.RepointActorReferences(actor, survivor);
        Assert.Equal(survivor, memberReq.ManuallyCompletedByPersonId);
        Assert.Equal(survivor, memberReq.OverriddenByPersonId);
    }

    private static void VerifyMemberUpdatesAndHistoryRepoint(
        GroupMember member,
        GroupTypeRole role,
        Guid retired,
        Guid surviving
    )
    {
        var badUpdate = member.Update(
            allowGuests: true,
            role.Id,
            GroupMemberStatus.Active,
            note: null,
            guestCount: -1,
            groupOrder: null,
            scheduleTemplateId: null,
            scheduleStartDate: null,
            scheduleReminderEmailOffsetDays: null,
            GroupCommunicationPreference.Sms,
            _now
        );
        Assert.True(badUpdate.IsFailure);

        Assert.True(
            member
                .Update(
                    allowGuests: true,
                    role.Id,
                    GroupMemberStatus.Inactive,
                    note: "Paused",
                    guestCount: 1,
                    groupOrder: 2,
                    scheduleTemplateId: null,
                    scheduleStartDate: null,
                    scheduleReminderEmailOffsetDays: 3,
                    GroupCommunicationPreference.Sms,
                    _now.AddDays(1)
                )
                .IsSuccess
        );
        _ = Assert.NotNull(member.InactiveDateTimeUtc);
        Assert.Equal(
            0,
            GroupMembershipPolicy.ComputeActiveOccupancyUnits([member], allowGuests: true)
        );

        Assert.True(
            member
                .Update(
                    allowGuests: true,
                    role.Id,
                    GroupMemberStatus.Active,
                    note: "Returned",
                    guestCount: 0,
                    groupOrder: 2,
                    scheduleTemplateId: null,
                    scheduleStartDate: null,
                    scheduleReminderEmailOffsetDays: 3,
                    GroupCommunicationPreference.Email,
                    _now.AddDays(2)
                )
                .IsSuccess
        );
        Assert.Null(member.InactiveDateTimeUtc);

        member.Archive(retired, _now.AddDays(3));
        var memberHist = GroupMemberHistorical.CreateSnapshot(member, role, _now.AddDays(3));
        memberHist.RepointPerson(retired, surviving);
        Assert.Equal(surviving, memberHist.PersonId);
        Assert.Equal(surviving, memberHist.ArchivedByPersonId);
    }

    private static void VerifyAttendanceRescheduleAndRepoint(
        GroupId groupId,
        GroupTypeId groupTypeId,
        Guid retired,
        Guid surviving
    )
    {
        var occ = AttendanceOccurrence.Create(
            groupId,
            groupTypeId,
            locationId: null,
            scheduleId: null,
            new DateOnly(2026, 7, 12)
        );
        var att = occ.ScheduleVolunteer(
            retired,
            campusId: null,
            scheduledByPersonId: retired,
            autoConfirm: false,
            _now
        );
        _ = occ.RecordPersonAttendance(
            retired,
            campusId: null,
            didAttend: true,
            isFirstTime: true,
            note: null,
            checkedInByPersonId: retired,
            countsAsWeekendService: false,
            _now
        );
        att.Reschedule(retired, autoConfirm: true, _now.AddHours(1));
        Assert.Equal(RsvpStatus.Yes, att.Rsvp);
        att.RepointPerson(retired, surviving);
        Assert.Equal(surviving, att.PersonId);
        Assert.Equal(surviving, att.ScheduledByPersonId);
        Assert.Equal(surviving, att.CheckedInByPersonId);
    }

    private static GroupTypeInput BuildGroupTypeInput(string name)
    {
        return new GroupTypeInput(
            name,
            Description: null,
            GroupTerm: "Group",
            GroupMemberTerm: "Member",
            IconCssClass: null,
            Order: 0,
            ShowInGroupList: true,
            ShowInNavigation: true,
            AllowMultipleLocations: true,
            IsMeetingStyleEnabled: false,
            GroupsRequireCampus: false,
            EnableGroupHistory: true,
            TakesAttendance: true,
            AttendanceCountsAsWeekendService: false,
            SendAttendanceReminder: false,
            GroupAttendanceRequiresLocation: false,
            GroupAttendanceRequiresSchedule: false,
            AttendanceRule.None,
            GroupCapacityRule.Soft,
            GroupLocationPickerMode.Named,
            EnableLocationSchedules: true,
            IsSchedulingEnabled: true,
            ScheduleConfirmationLogic.Ask,
            ScheduleReminderEmailOffsetDays: null,
            PurposeValueId: null,
            InheritedGroupTypeId: null
        );
    }

    private static GroupInput BuildGroupInput(
        string name,
        Guid? coordinatorId,
        Guid? administratorId,
        bool allowGuests
    )
    {
        return new GroupInput(
            name,
            ParentGroupId: null,
            CampusId: null,
            ScheduleId: null,
            Description: null,
            PhotoId: null,
            MeetingStyle: null,
            IsActive: true,
            IsPublic: true,
            Order: 0,
            AllowGuests: allowGuests,
            GroupCapacity: 10,
            StatusValueId: null,
            RequiredSignatureDocumentTemplateId: null,
            SchedulingMustMeetRequirements: false,
            ScheduleCoordinatorPersonId: coordinatorId,
            GroupAdministratorPersonId: administratorId,
            DisableScheduling: false,
            DisableScheduleToolboxAccess: false,
            RsvpReminderOffsetDays: null
        );
    }
}
