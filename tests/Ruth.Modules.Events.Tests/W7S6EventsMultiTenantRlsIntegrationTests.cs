using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Events.Tests;

[Collection(EventsPostgresFixtures.Name)]
public sealed class W7S6EventsMultiTenantRlsIntegrationTests(
    EventsPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task ForceRls_CollidingCalendarSlugs_SignalRGroupPartitioning_AndWhiteLabelIcs_IsolateTenants()
    {
        var dbConn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EventsTestHostFactory.BuildAndMigrateAsync(dbConn);

        await W7S6EventsRlsTestHelper.AssertAllEventsTablesHaveForceRlsAndFailClosedAsync(
            dbConn,
            provider
        );

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var sessionA = await SeedTenantEventsAndSessionAsync(
            provider,
            tenantA,
            "alpha-events",
            "Alpha Relief Town Hall"
        );
        var sessionB = await SeedTenantEventsAndSessionAsync(
            provider,
            tenantB,
            "beta-events",
            "Beta Shelter Briefing"
        );

        await VerifyCrossTenantCalendarFeedAndSessionIsolationAsync(
            provider,
            tenantA,
            sessionA,
            tenantB,
            sessionB
        );
        W7S6EventsRlsTestHelper.VerifySignalRGroupNamesRequireAndPartitionByTenant(
            tenantA,
            tenantB,
            sessionA.Id
        );
    }

    private static async Task<InteractiveSessionDetailResponse> SeedTenantEventsAndSessionAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string eventTitle
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var seeder = scope.ServiceProvider.GetRequiredService<EventsTenantSeedContributor>();
            await seeder.SeedTenantAsync(tenantId);

            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var publicCal = Assert.Single(
                (
                    await queries.QueryAsync<
                        GetEventCalendarsQuery,
                        IReadOnlyList<EventCalendarResponse>
                    >(new GetEventCalendarsQuery(PublicOnly: true, ActiveOnly: true))
                ).Value
            );
            Assert.Equal("public-events", publicCal.Slug);

            var item = (
                await commands.SendAsync<CreateEventItemCommand, EventItemResponse>(
                    new CreateEventItemCommand(
                        new EventItemSpec(
                            eventTitle,
                            "annual-town-hall",
                            [publicCal.Id],
                            Summary: $"Town hall for {slug}"
                        )
                    )
                )
            ).Value;

            _ = await commands.SendAsync<
                AddEventItemOccurrenceCommand,
                EventItemOccurrenceResponse
            >(
                new AddEventItemOccurrenceCommand(
                    item.Id,
                    new EventItemOccurrenceSpec(
                        LocationDescription: "Main Auditorium",
                        CustomStartUtc: DateTimeOffset.UtcNow.AddDays(3),
                        CustomEndUtc: DateTimeOffset.UtcNow.AddDays(3).AddHours(2),
                        TimeZoneId: "UTC"
                    )
                )
            );

            return (
                await commands.SendAsync<
                    CreateInteractiveSessionCommand,
                    InteractiveSessionDetailResponse
                >(new CreateInteractiveSessionCommand($"{eventTitle} Live Q&A"))
            ).Value;
        }
    }

    private static async Task VerifyCrossTenantCalendarFeedAndSessionIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        InteractiveSessionDetailResponse sessionA,
        TenantId tenantB,
        InteractiveSessionDetailResponse sessionB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantA, "alpha-events", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var feedA = (
                await queries.QueryAsync<GetCalendarIcsFeedQuery, CalendarFeedPayloadResponse>(
                    new GetCalendarIcsFeedQuery(
                        "public-events",
                        DateTimeOffset.UtcNow.AddDays(-1),
                        DateTimeOffset.UtcNow.AddDays(14)
                    )
                )
            ).Value;

            Assert.Contains("Alpha Relief Town Hall", feedA.IcsContent, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Beta Shelter Briefing",
                feedA.IcsContent,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("Ruth", feedA.IcsContent, StringComparison.OrdinalIgnoreCase);

            var crossStage = await queries.QueryAsync<
                GetInteractiveSessionStageQuery,
                InteractiveStageProjectionResponse
            >(new GetInteractiveSessionStageQuery(sessionB.JoinCode));
            Assert.True(crossStage.IsFailure);
        }

        using (AmbientTenantContext.BeginScope(tenantB, "beta-events", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var sessionsB = (
                await queries.QueryAsync<
                    GetInteractiveSessionsQuery,
                    IReadOnlyList<InteractiveSessionDetailResponse>
                >(new GetInteractiveSessionsQuery())
            ).Value;
            var singleB = Assert.Single(sessionsB);
            Assert.Equal(sessionB.Id, singleB.Id);
            Assert.NotEqual(sessionA.Id, singleB.Id);
        }
    }
}
