using System.Text;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Xunit;

namespace Ruth.Modules.Events.Tests;

public sealed class W4S7CalendarsIcsAndInteractiveDomainTests
{
    [Fact]
    public void IcsCalendarFeedBuilder_FoldsAt75Octets_AndEscapesRfc5545SpecialCharacters()
    {
        var stamp = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var start = new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 20, 0, 0, TimeSpan.Zero);

        var longDescription =
            "Annual summit; workshops, Q&A\nLine two with UTF-8 accents éüñ and extra details exceeding seventy-five octets easily.";
        var ics = IcsCalendarFeedBuilder.BuildFeed(
            "Public Events, Workshops & Summits",
            "Community calendar",
            [
                new IcsCalendarEventEntry(
                    "evt-001@ruth.events",
                    "Community Summit; Town Hall, Part 1",
                    start,
                    end,
                    stamp,
                    longDescription,
                    "Main Hall, Room 204",
                    "https://example.org/events/summit",
                    ["Members", "Volunteers"],
                    IsCancelled: false
                ),
            ]
        );

        Assert.Contains("BEGIN:VCALENDAR\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("VERSION:2.0\r\n", ics, StringComparison.Ordinal);
        Assert.Contains("STATUS:CONFIRMED\r\n", ics, StringComparison.Ordinal);
        Assert.Contains(
            @"SUMMARY:Community Summit\; Town Hall\, Part 1",
            ics,
            StringComparison.Ordinal
        );
        Assert.Contains("END:VCALENDAR\r\n", ics, StringComparison.Ordinal);

        var lines = ics.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
            Assert.True(
                Encoding.UTF8.GetByteCount(rawLine) <= 75,
                $"RFC 5545 content line exceeded 75 UTF-8 octets: {rawLine}"
            );
        }
    }

    [Fact]
    public void EventCalendar_And_EventItem_EnforceSlugs_Occurrences_AndPersonRepoint()
    {
        var badCal = EventCalendar.Create(new EventCalendarSpec("", "BAD SLUG!"));
        Assert.True(badCal.IsFailure);

        var goodCal = EventCalendar.Create(
            new EventCalendarSpec("Public Calendar", "public-calendar", "Public feed")
        );
        Assert.True(goodCal.IsSuccess);

        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();

        var itemRes = EventItem.Create(
            new EventItemSpec(
                "Summer Workshop",
                "summer-workshop",
                [goodCal.Value.Id.Value],
                ApprovedByPersonId: retiredPersonId,
                Audiences: [new EventItemAudienceDto(AudienceDefinedValueId: null, "Students")]
            ),
            now
        );
        Assert.True(itemRes.IsSuccess);

        var badOcc = itemRes.Value.AddOccurrence(new EventItemOccurrenceSpec());
        Assert.True(badOcc.IsFailure);

        var goodOcc = itemRes.Value.AddOccurrence(
            new EventItemOccurrenceSpec(
                CustomStartUtc: now.AddDays(5),
                CustomEndUtc: now.AddDays(5).AddHours(2),
                ContactPersonId: retiredPersonId
            )
        );
        Assert.True(goodOcc.IsSuccess);
        var updatedOcc = itemRes.Value.UpdateOccurrence(
            goodOcc.Value.Id,
            new EventItemOccurrenceSpec(
                CustomStartUtc: now.AddDays(6),
                CustomEndUtc: now.AddDays(6).AddHours(3),
                ContactPersonId: retiredPersonId
            )
        );
        Assert.True(updatedOcc.IsSuccess);
        Assert.True(
            itemRes
                .Value.UpdateOccurrence(
                    EventItemOccurrenceId.Create(),
                    new EventItemOccurrenceSpec(CustomStartUtc: now.AddDays(6))
                )
                .IsFailure
        );
        Assert.True(itemRes.Value.RepointPerson(retiredPersonId, survivingPersonId));
        Assert.Equal(survivingPersonId, itemRes.Value.ApprovedByPersonId);
        Assert.Equal(survivingPersonId, goodOcc.Value.ContactPersonId);
    }

    [Fact]
    public void InteractiveSession_EnforcesLiveState_SingleVotePerParticipant_Moderation_AndTallies()
    {
        var moderatorOld = Guid.CreateVersion7();
        var moderatorNew = Guid.CreateVersion7();
        var sessionRes = InteractiveSession.Create(
            "Quarterly Town Hall",
            moderatorPersonId: moderatorOld,
            initialActions:
            [
                new InteractiveActionSpec(
                    "Opening Poll",
                    "Preferred Workshop Time?",
                    InteractiveActionType.MultipleChoicePoll,
                    ["Morning", "Evening"],
                    DisplayOrder: 1,
                    AllowMultipleResponses: false
                ),
                new InteractiveActionSpec(
                    "Live Q&A",
                    "Submit questions for the panel",
                    InteractiveActionType.OpenTextQa,
                    DisplayOrder: 2,
                    RequireModeration: true
                ),
            ]
        );
        Assert.True(sessionRes.IsSuccess);
        var session = sessionRes.Value;
        Assert.Equal(6, session.JoinCode.Length);

        var actions = session.Actions.OrderBy(static a => a.DisplayOrder).ToArray();
        var now = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero);

        var beforeLive = session.SubmitResponse(
            actions[0].Id,
            "tok-1",
            "Morning",
            textResponse: null,
            responderPersonId: null,
            now
        );
        Assert.True(beforeLive.IsFailure);

        _ = session.StartOccurrence(eventItemOccurrenceId: null, branchId: null, now);
        Assert.Equal(InteractiveSessionStatus.Live, session.Status);

        Assert.True(
            session
                .SubmitResponse(
                    actions[0].Id,
                    "tok-1",
                    "morning",
                    textResponse: null,
                    responderPersonId: moderatorOld,
                    now
                )
                .IsSuccess
        );
        Assert.True(
            session
                .SubmitResponse(
                    actions[0].Id,
                    "tok-1",
                    "Evening",
                    textResponse: null,
                    responderPersonId: null,
                    now
                )
                .IsFailure
        );
        Assert.True(
            session
                .SubmitResponse(
                    actions[0].Id,
                    "tok-2",
                    "Evening",
                    textResponse: null,
                    responderPersonId: null,
                    now
                )
                .IsSuccess
        );

        var pollStage = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        Assert.Equal(2, pollStage.TotalResponsesForActiveAction);
        Assert.All(pollStage.PollTallies, static t => Assert.Equal(50.00m, t.VotePercent));

        _ = session.SetActiveAction(actions[1].Id);
        var qaResp = session.SubmitResponse(
            actions[1].Id,
            "tok-qa-1",
            selectedOption: null,
            textResponse: "When does fall scholarship intake open?",
            responderPersonId: null,
            now
        );
        Assert.True(qaResp.IsSuccess);
        Assert.Empty(
            InteractiveStageProjectionCalculator
                .BuildStageProjection(session)
                .ApprovedAudienceMessages
        );

        _ = session.ModerateResponse(
            qaResp.Value.Id,
            InteractiveModerationStatus.Approved,
            incrementUpvote: true
        );
        var moderatedStage = InteractiveStageProjectionCalculator.BuildStageProjection(session);
        var approvedMsg = Assert.Single(moderatedStage.ApprovedAudienceMessages);
        Assert.Equal(1, approvedMsg.Upvotes);

        Assert.True(session.RepointPerson(moderatorOld, moderatorNew));
        _ = session.EndSession(now.AddHours(1));
        Assert.Equal(InteractiveSessionStatus.Ended, session.Status);
    }
}
