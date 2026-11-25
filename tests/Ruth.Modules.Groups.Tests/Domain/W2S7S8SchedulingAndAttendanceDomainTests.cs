using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Scheduling;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Domain;

public sealed class W2S7S8SchedulingAndAttendanceDomainTests
{
    private static readonly DateTimeOffset _now = new(2026, 7, 5, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ServingScheduleEvaluator_CoversAllCapacityThresholdsAndEligibility()
    {
        Assert.Equal(
            ScheduleCapacityStatus.Unconfigured,
            ServingScheduleEvaluator.EvaluateCapacityStatus(
                0,
                minimumCapacity: null,
                desiredCapacity: null,
                maximumCapacity: null
            )
        );
        Assert.Equal(
            ScheduleCapacityStatus.UnderMinimum,
            ServingScheduleEvaluator.EvaluateCapacityStatus(1, 2, 4, 6)
        );
        Assert.Equal(
            ScheduleCapacityStatus.BelowDesired,
            ServingScheduleEvaluator.EvaluateCapacityStatus(3, 2, 4, 6)
        );
        Assert.Equal(
            ScheduleCapacityStatus.Optimal,
            ServingScheduleEvaluator.EvaluateCapacityStatus(4, 2, 4, 6)
        );
        Assert.Equal(
            ScheduleCapacityStatus.AtMaximum,
            ServingScheduleEvaluator.EvaluateCapacityStatus(6, 2, 4, 6)
        );
        Assert.Equal(
            ScheduleCapacityStatus.OverMaximum,
            ServingScheduleEvaluator.EvaluateCapacityStatus(7, 2, 4, 6)
        );
        Assert.True(ServingScheduleEvaluator.WouldExceedMaximumCapacity(6, 6));
        Assert.False(ServingScheduleEvaluator.WouldExceedMaximumCapacity(5, 6));

        Assert.True(
            ServingScheduleEvaluator.IsCandidateEligible(
                groupSchedulingEnabled: true,
                dateExcludedByGroupType: false,
                hasPersonBlackout: false,
                hasRequirementGap: false,
                hasDoubleBooking: false
            )
        );
        Assert.False(
            ServingScheduleEvaluator.IsCandidateEligible(
                groupSchedulingEnabled: true,
                dateExcludedByGroupType: false,
                hasPersonBlackout: true,
                hasRequirementGap: false,
                hasDoubleBooking: false
            )
        );
        Assert.False(
            ServingScheduleEvaluator.IsCandidateEligible(
                groupSchedulingEnabled: true,
                dateExcludedByGroupType: true,
                hasPersonBlackout: false,
                hasRequirementGap: false,
                hasDoubleBooking: false
            )
        );
    }

    [Fact]
    public void ScheduleTemplates_Configs_AndExclusions_EnforceInvariants()
    {
        var tpl = GroupMemberScheduleTemplate
            .Create(
                "Every Other Week",
                Guid.CreateVersion7(),
                groupTypeId: null,
                "Biweekly",
                intervalWeeks: 2
            )
            .Value;
        var anchor = new DateOnly(2026, 7, 5);
        Assert.True(tpl.MatchesDate(anchor, anchor));
        Assert.False(tpl.MatchesDate(anchor, anchor.AddDays(7)));
        Assert.True(tpl.MatchesDate(anchor, anchor.AddDays(14)));
        Assert.False(tpl.MatchesDate(anchor, anchor.AddDays(-7)));

        var badConfig = GroupLocationScheduleConfig.Create(
            GroupLocationId.Create(),
            Guid.CreateVersion7(),
            "Nursery",
            minimumCapacity: 5,
            desiredCapacity: 3,
            maximumCapacity: 8,
            confirmationDetails: null,
            reminderDetails: null
        );
        Assert.True(badConfig.IsFailure);
        Assert.Equal(GroupsErrors.InvalidScheduleCapacities.Code, badConfig.PrimaryError.Code);

        var badPersonEx = PersonScheduleExclusion.Create(
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 5),
            "Bad Range",
            groupId: null,
            parentExclusionId: null
        );
        Assert.True(badPersonEx.IsFailure);

        var groupId = GroupId.Create();
        var ex = PersonScheduleExclusion
            .Create(
                Guid.CreateVersion7(),
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 15),
                "Vacation",
                groupId,
                parentExclusionId: null
            )
            .Value;
        Assert.True(ex.Covers(new DateOnly(2026, 8, 8), groupId));
        Assert.False(ex.Covers(new DateOnly(2026, 8, 8), GroupId.Create()));
    }

    [Fact]
    public void ReportingWeekCalculator_AndAttendanceOccurrence_StageOutboxEventsAndRsvpsCleanly()
    {
        // Monday 2026-07-06 -> Sunday 2026-07-12; Sunday 2026-07-12 -> Sunday 2026-07-12
        Assert.Equal(
            new DateOnly(2026, 7, 12),
            ReportingWeekCalculator.ComputeWeekEndDate(new DateOnly(2026, 7, 6))
        );
        Assert.Equal(
            new DateOnly(2026, 7, 12),
            ReportingWeekCalculator.ComputeWeekEndDate(new DateOnly(2026, 7, 12))
        );
        Assert.Equal(
            new DateOnly(2026, 7, 10),
            ReportingWeekCalculator.ComputeWeekEndDate(new DateOnly(2026, 7, 6), DayOfWeek.Friday)
        );
        Assert.Equal(20260712, ReportingWeekCalculator.ComputeDateKey(new DateOnly(2026, 7, 12)));

        var occ = AttendanceOccurrence.Create(
            GroupId.Create(),
            GroupTypeId.Create(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 7, 12)
        );
        var personId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();

        var firstSave = occ.RecordPersonAttendance(
            personId,
            branchId,
            didAttend: true,
            isFirstTime: true,
            "Checked in",
            checkedInByPersonId: null,
            countsTowardHeadlineAttendance: true,
            _now
        );
        Assert.True(firstSave.IsSuccess);
        var evt = Assert.Single(occ.IntegrationEvents);
        var attendedEvt = Assert.IsType<PersonAttendedIntegrationEvent>(evt);
        Assert.Equal(personId, attendedEvt.PersonId);
        Assert.True(attendedEvt.IsFirstTime);
        Assert.True(attendedEvt.CountsTowardHeadlineAttendance);

        occ.ClearIntegrationEvents();
        // Re-saving same attendee with updated note does NOT emit duplicate event
        _ = occ.RecordPersonAttendance(
            personId,
            branchId,
            didAttend: true,
            isFirstTime: false,
            "Updated note",
            checkedInByPersonId: null,
            countsTowardHeadlineAttendance: true,
            _now.AddMinutes(5)
        );
        Assert.Empty(occ.IntegrationEvents);

        // Marking DidNotOccur clears DidAttend and AnonymousAttendanceCount
        var meta = occ.UpdateOccurrenceMetadata(
            didNotOccur: true,
            anonymousAttendanceCount: 5,
            "Holiday",
            name: null
        );
        Assert.True(meta.IsSuccess);
        Assert.Null(occ.AnonymousAttendanceCount);
        Assert.False(firstSave.Value.DidAttend);

        // RSVP lifecycle
        var volunteer = occ.ScheduleVolunteer(
            Guid.CreateVersion7(),
            branchId,
            Guid.CreateVersion7(),
            autoConfirm: false,
            _now
        );
        Assert.False(string.IsNullOrWhiteSpace(volunteer.RsvpToken));
        Assert.Equal(RsvpStatus.Unknown, volunteer.Rsvp);
        Assert.True(
            volunteer
                .ApplyRsvp(
                    RsvpStatus.Yes,
                    declineReasonValueId: null,
                    declineReasonNote: null,
                    _now.AddHours(1)
                )
                .IsSuccess
        );
        Assert.True(volunteer.ScheduledToAttend);
        Assert.True(
            volunteer
                .ApplyRsvp(RsvpStatus.No, Guid.CreateVersion7(), "Traveling", _now.AddHours(2))
                .IsSuccess
        );
        Assert.False(volunteer.ScheduledToAttend);
        Assert.Equal("Traveling", volunteer.DeclineReasonNote);
    }
}
