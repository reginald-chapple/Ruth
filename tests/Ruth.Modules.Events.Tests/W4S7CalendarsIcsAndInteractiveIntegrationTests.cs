using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure.Realtime;
using Ruth.Modules.Events.Slices.Events;
using Ruth.Modules.Events.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Events.Tests;

[Collection(EventsPostgresFixtures.Name)]
public sealed class W4S7CalendarsIcsAndInteractiveIntegrationTests(
    EventsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task Calendars_ScheduleExpandedIcsFeed_InteractiveLiveSession_AndPersonMerge_PersistEndToEnd()
    {
        var broadcastSpy = new W4S7InteractiveBroadcastSpy();
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EventsTestHostFactory.BuildAndMigrateAsync(
            conn,
            services => _ = services.AddSingleton<IInteractiveSessionBroadcaster>(broadcastSpy)
        );

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();

        var calendarId = await VerifySeededCalendarsAndCreateScheduleEventAsync(
            commands,
            queries,
            retiredPersonId
        );
        await VerifyIcsFeedContainsScheduleAndCustomOccurrencesAsync(queries, calendarId);
        await VerifyInteractiveSessionAndPersonMergeAsync(
            scope.ServiceProvider,
            commands,
            queries,
            broadcastSpy,
            retiredPersonId,
            survivingPersonId
        );
    }

    private static async Task<Guid> VerifySeededCalendarsAndCreateScheduleEventAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid contactPersonId
    )
    {
        var calRes = await queries.QueryAsync<
            GetEventCalendarsQuery,
            IReadOnlyList<EventCalendarResponse>
        >(new GetEventCalendarsQuery(PublicOnly: true, ActiveOnly: true));
        Assert.True(calRes.IsSuccess);
        var publicCal = Assert.Single(calRes.Value);
        Assert.Equal("public-events", publicCal.Slug);

        var schedRes = await commands.SendAsync<CreateScheduleCommand, ScheduleResponse>(
            new CreateScheduleCommand(
                Name: "Weekly Community Meetup",
                WeeklyDayOfWeek: DayOfWeek.Wednesday,
                WeeklyTimeOfDay: new TimeOnly(18, 30),
                TimeZoneId: "UTC"
            )
        );
        Assert.True(schedRes.IsSuccess);

        var itemRes = await commands.SendAsync<CreateEventItemCommand, EventItemResponse>(
            new CreateEventItemCommand(
                new EventItemSpec(
                    "Community Forum",
                    "community-forum-2026",
                    [publicCal.Id],
                    Summary: "Weekly open community forum",
                    Audiences: [new EventItemAudienceDto(AudienceDefinedValueId: null, "Members")]
                )
            )
        );
        Assert.True(itemRes.IsSuccess);

        var occRes = await commands.SendAsync<
            AddEventItemOccurrenceCommand,
            EventItemOccurrenceResponse
        >(
            new AddEventItemOccurrenceCommand(
                itemRes.Value.Id,
                new EventItemOccurrenceSpec(
                    LocationDescription: "Civic Center Room A",
                    ScheduleId: schedRes.Value.Id,
                    CustomStartUtc: DateTimeOffset.UtcNow.AddDays(2),
                    CustomEndUtc: DateTimeOffset.UtcNow.AddDays(2).AddHours(2),
                    TimeZoneId: "UTC",
                    ContactPersonId: contactPersonId
                )
            )
        );
        Assert.True(occRes.IsSuccess);
        return publicCal.Id;
    }

    private static async Task VerifyIcsFeedContainsScheduleAndCustomOccurrencesAsync(
        IQueryDispatcher queries,
        Guid calendarId
    )
    {
        var feedRes = await queries.QueryAsync<
            GetCalendarIcsFeedQuery,
            CalendarFeedPayloadResponse
        >(
            new GetCalendarIcsFeedQuery(
                "public-events",
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(21)
            )
        );
        Assert.True(feedRes.IsSuccess);
        Assert.Contains("BEGIN:VCALENDAR", feedRes.Value.IcsContent, StringComparison.Ordinal);
        Assert.Contains(
            "SUMMARY:Community Forum",
            feedRes.Value.IcsContent,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "LOCATION:Civic Center Room A",
            feedRes.Value.IcsContent,
            StringComparison.Ordinal
        );

        var byGuidRes = await queries.QueryAsync<
            GetCalendarIcsFeedQuery,
            CalendarFeedPayloadResponse
        >(new GetCalendarIcsFeedQuery(calendarId.ToString("D")));
        Assert.True(byGuidRes.IsSuccess);
    }

    private static async Task VerifyInteractiveSessionAndPersonMergeAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        W4S7InteractiveBroadcastSpy spy,
        Guid retiredPersonId,
        Guid survivingPersonId
    )
    {
        var sessionRes = await commands.SendAsync<
            CreateInteractiveSessionCommand,
            InteractiveSessionDetailResponse
        >(
            new CreateInteractiveSessionCommand(
                "Live Town Hall Session",
                ModeratorPersonId: retiredPersonId,
                InitialActions:
                [
                    new InteractiveActionSpec(
                        "Budget Poll",
                        "Approve annual community aid expansion?",
                        InteractiveActionType.MultipleChoicePoll,
                        ["Yes", "No"]
                    ),
                ]
            )
        );
        Assert.True(sessionRes.IsSuccess);

        var startRes = await commands.SendAsync<
            StartInteractiveSessionOccurrenceCommand,
            InteractiveSessionDetailResponse
        >(new StartInteractiveSessionOccurrenceCommand(sessionRes.Value.Id));
        Assert.True(startRes.IsSuccess);
        Assert.Equal(InteractiveSessionStatus.Live, startRes.Value.Status);

        var actionId = sessionRes.Value.Actions[0].Id;
        var voteRes = await commands.SendAsync<
            SubmitInteractiveResponseCommand,
            InteractiveStageProjectionResponse
        >(
            new SubmitInteractiveResponseCommand(
                sessionRes.Value.JoinCode,
                actionId,
                "participant-alpha",
                SelectedOption: "Yes",
                ResponderPersonId: retiredPersonId
            )
        );
        Assert.True(voteRes.IsSuccess);
        Assert.Equal(1, voteRes.Value.TotalResponsesForActiveAction);
        Assert.True(spy.Projections.Count >= 2);

        var innerConsumer = sp.GetRequiredService<EventsPersonMergedConsumer>();
        Assert.NotNull(innerConsumer);
        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingPersonId,
                retiredPersonId,
                [],
                HadMultipleDistinctEmails: false
            )
        );

        var sessionsAfter = await queries.QueryAsync<
            GetInteractiveSessionsQuery,
            IReadOnlyList<InteractiveSessionDetailResponse>
        >(new GetInteractiveSessionsQuery());
        Assert.True(sessionsAfter.IsSuccess);
        var updatedSession = Assert.Single(sessionsAfter.Value);
        Assert.Equal(survivingPersonId, updatedSession.ModeratorPersonId);
        Assert.Equal(survivingPersonId, updatedSession.AllResponses[0].ResponderPersonId);
    }
}
