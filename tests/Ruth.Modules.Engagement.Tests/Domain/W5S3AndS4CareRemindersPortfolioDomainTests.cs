using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Domain;

public sealed class W5S3AndS4CareRemindersPortfolioDomainTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = new(2026, 11, 25);

    [Fact]
    public void SupportRequest_Moderation_SupportCounter_Comments_AutoFlag_AndExpiration_Work()
    {
        var category = SupportCategory
            .Create(
                new SupportCategorySpec(
                    "Mutual Aid",
                    "mutual-aid",
                    RequiresModeration: true,
                    DefaultExpirationDays: 7,
                    AllowPublicCommunityWall: true,
                    AllowComments: true,
                    AutoFlagHoldThreshold: 2
                )
            )
            .Value;

        var req = SupportRequest
            .Submit(
                category,
                new SupportRequestIntakeSpec(
                    "Alex",
                    "Need grocery delivery assistance",
                    LastName: "Rivera",
                    Email: "alex@example.org",
                    VisibilityLevel: SupportVisibilityLevel.PublicCommunityWall,
                    IsUrgent: true
                ),
                _now
            )
            .Value;

        Assert.Equal(SupportModerationState.PendingReview, req.ModerationState);

        Assert.True(req.RecordSupport(isCareTeamMember: false, _now).IsFailure);
        Assert.True(req.RecordSupport(isCareTeamMember: true, _now).IsSuccess);
        Assert.True(
            req.AddComment(
                "Coordinator",
                "Internal triage note",
                authorPersonId: null,
                isInternalCareNote: true,
                _now
            ).IsSuccess
        );
        Assert.True(
            req.AddComment(
                "Neighbor",
                "Public note",
                authorPersonId: null,
                isInternalCareNote: false,
                _now
            ).IsFailure
        );

        Assert.True(
            req.Moderate(
                category,
                SupportModerationDecision.Approve,
                Guid.CreateVersion7(),
                editedText: null,
                overrideVisibility: null,
                assignedCoordinatorId: null,
                _now
            ).IsSuccess
        );
        Assert.True(req.RecordSupport(isCareTeamMember: false, _now).IsSuccess);
        Assert.Equal(2, req.CommunitySupportCount);
        Assert.True(
            req.AddComment(
                "Neighbor",
                "Happy to help!",
                authorPersonId: null,
                isInternalCareNote: false,
                _now
            ).IsSuccess
        );

        req.FlagForReview(category.AutoFlagHoldThreshold);
        Assert.Equal(SupportModerationState.Approved, req.ModerationState);
        req.FlagForReview(category.AutoFlagHoldThreshold);
        Assert.Equal(SupportModerationState.Flagged, req.ModerationState);

        Assert.False(req.ArchiveIfExpired(_now.AddDays(3)));
        Assert.True(req.ArchiveIfExpired(_now.AddDays(8)));
        Assert.Equal(SupportModerationState.Archived, req.ModerationState);

        var oldPerson = Guid.CreateVersion7();
        var newPerson = Guid.CreateVersion7();
        var catWithCoord = SupportCategory
            .Create(
                new SupportCategorySpec("Care", "care-cat", DefaultCoordinatorPersonId: oldPerson)
            )
            .Value;
        var req2 = SupportRequest
            .Submit(
                catWithCoord,
                new SupportRequestIntakeSpec("Sam", "Help", RequesterPersonId: oldPerson),
                _now
            )
            .Value;
        _ = req2.Moderate(
            catWithCoord,
            SupportModerationDecision.Approve,
            oldPerson,
            editedText: null,
            overrideVisibility: null,
            assignedCoordinatorId: oldPerson,
            _now
        );
        _ = req2.AddComment("Care", "Note", oldPerson, isInternalCareNote: true, _now);
        Assert.True(catWithCoord.RepointPerson(oldPerson, newPerson));
        Assert.True(req2.RepointPerson(oldPerson, newPerson));
    }

    [Fact]
    public void Reminder_RenewalCycles_Notifications_AndFollowing_Work()
    {
        var type = ReminderType
            .Create(
                new ReminderTypeSpec(
                    "Check-In",
                    ShouldAutoCompleteWhenNotified: true,
                    EntityTypeKey: "Ruth.Crm.Person"
                )
            )
            .Value;

        Assert.True(
            Reminder
                .Create(
                    type,
                    Guid.CreateVersion7(),
                    "Ruth.Groups.Group",
                    Guid.CreateVersion7(),
                    "Group",
                    "Note",
                    _today,
                    renewPeriodDays: null,
                    renewMaxCount: null
                )
                .IsFailure
        );

        var assignee = Guid.CreateVersion7();
        var reminder = Reminder
            .Create(
                type,
                assignee,
                "Ruth.Crm.Person",
                Guid.CreateVersion7(),
                "Jordan Hayes",
                "Follow up",
                _today,
                renewPeriodDays: 7,
                renewMaxCount: 1
            )
            .Value;

        Assert.True(reminder.RecordNotificationAndAdvance(type, _today, _now));
        Assert.False(reminder.IsComplete);
        Assert.Equal(_today.AddDays(7), reminder.ReminderDate);
        Assert.Equal(1, reminder.RenewCurrentCount);

        Assert.True(
            reminder.RecordNotificationAndAdvance(type, _today.AddDays(7), _now.AddDays(7))
        );
        Assert.True(reminder.IsComplete);

        var notif = NotificationMessage.Create(
            assignee,
            "Due",
            "Follow up",
            NotificationCategory.Reminder
        );
        notif.MarkRead(_now);
        Assert.True(notif.IsRead);

        var sub = FollowingSubscription.Create(
            assignee,
            "Ruth.Crm.Person",
            Guid.CreateVersion7(),
            "Jordan",
            purposeKey: null
        );
        Assert.True(sub.IsSuccess);
    }

    [Fact]
    public void ConstituentContact_AndTouchpointCadenceScheduler_ComputeHealthAndTransitions()
    {
        var owner = Guid.CreateVersion7();
        var constituent = Guid.CreateVersion7();

        Assert.True(
            ConstituentContact.Create(new ConstituentContactSpec(owner, owner), _today).IsFailure
        );

        var contact = ConstituentContact
            .Create(
                new ConstituentContactSpec(
                    owner,
                    constituent,
                    "Steward",
                    ContactRelationshipStrength.Established,
                    OutreachCadenceDays: 30,
                    ConnectionNoteCadenceDays: 90
                ),
                _today
            )
            .Value;

        Assert.Equal(_today.AddDays(30), contact.NextScheduledTouchpointDate);
        Assert.Equal(ConstituentHealthStatus.Healthy, contact.HealthStatus);
        Assert.Equal(80, contact.HealthScore);

        contact.RefreshScheduleAndHealth(_today.AddDays(45));
        Assert.Equal(ConstituentHealthStatus.Overdue, contact.HealthStatus);
        Assert.True(contact.HealthScore < 80);

        contact.RefreshScheduleAndHealth(_today.AddDays(70));
        Assert.Equal(ConstituentHealthStatus.CriticalLapse, contact.HealthStatus);

        var tp = Touchpoint
            .Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                TouchpointChannel.PhoneCall,
                _today.AddDays(30)
            )
            .Value;

        Assert.True(tp.Snooze(_today.AddDays(35), "Rescheduled").IsSuccess);
        Assert.Equal(TouchpointStatus.Snoozed, tp.Status);
        Assert.Equal(1, tp.SnoozeCount);

        Assert.True(
            tp.Complete(
                TouchpointChannel.InPersonMeeting,
                TouchpointOutcomeSentiment.Positive,
                "Great coffee",
                _now
            ).IsSuccess
        );
        Assert.True(
            tp.Complete(
                TouchpointChannel.PhoneCall,
                TouchpointOutcomeSentiment.Neutral,
                summary: null,
                _now
            ).IsFailure
        );

        contact.RecordCompletedTouchpoint(
            tp.Kind,
            tp.Channel,
            TouchpointOutcomeSentiment.Positive,
            ContactRelationshipStrength.Strong,
            _now,
            _today
        );
        Assert.Equal(100, contact.HealthScore);
        Assert.Equal(ConstituentHealthStatus.Healthy, contact.HealthStatus);

        Assert.Equal(
            _today.AddDays(90),
            TouchpointCadenceScheduler.ComputeNextConnectionNoteDate(
                90,
                lastNoteAtUtc: null,
                _today
            )
        );
        var tpSkip = Touchpoint
            .Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                TouchpointChannel.Email,
                _today.AddDays(30)
            )
            .Value;
        Assert.True(tpSkip.Skip("Out of town").IsSuccess);
        Assert.Equal(TouchpointStatus.Skipped, tpSkip.Status);
        Assert.True(tpSkip.Skip("Again").IsFailure);
    }
}
