using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.Modules.Cms.Tests.Fixtures;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Cms.Tests.Infrastructure;

[Collection(CmsFixtures.Name)]
public sealed class W6S3CmsPersonalizationIntegrationTests(CmsContainersFixture containers)
{
    [Fact]
    public async Task W6_S3_BufferedInteractions_RedisSegments_AdaptiveSaturation_AndMerge_Work()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        await using var redis = await containers.ConnectRedisAsync();
        await using var sp = await CmsTestHostFactory.CreateAsync(dbConn, redis);
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var personId = Guid.CreateVersion7();
        await VerifyInteractionsAndRedisSegmentCacheAsync(
            commands,
            scope.ServiceProvider,
            personId
        );
        await VerifyAdaptiveSaturationAsync(commands, queries, personId);
        await VerifyPersonMergedConsumerAsync(scope.ServiceProvider, personId);
    }

    private static async Task VerifyInteractionsAndRedisSegmentCacheAsync(
        ICommandDispatcher commands,
        IServiceProvider sp,
        Guid personId
    )
    {
        _ = (
            await commands.SendAsync<CreateInteractionChannelCommand, InteractionChannelResponse>(
                new CreateInteractionChannelCommand("Portal Web", "portal-web")
            )
        ).Value;

        var ingest = (
            await commands.SendAsync<IngestInteractionsCommand, InteractionBufferStatusResponse>(
                new IngestInteractionsCommand(
                    [
                        new("portal-web", "home", "Home", "View", personId, "sess-1", "Visit 1"),
                        new(
                            "portal-web",
                            "events",
                            "Events",
                            "View",
                            personId,
                            "sess-1",
                            "Visit 2"
                        ),
                    ],
                    FlushImmediately: false
                )
            )
        ).Value;
        Assert.Equal(2, ingest.EnqueuedCount);

        var flushed = (
            await commands.SendAsync<
                FlushInteractionBufferCommand,
                InteractionBufferStatusResponse
            >(new FlushInteractionBufferCommand())
        ).Value;
        Assert.True(flushed.TotalFlushedCount >= 2);

        var seg = (
            await commands.SendAsync<
                CreatePersonalizationSegmentCommand,
                PersonalizationSegmentResponse
            >(
                new CreatePersonalizationSegmentCommand(
                    "Engaged Members",
                    "engaged-members",
                    FilterSpec: new PersonalizationSegmentFilterSpec(
                        InteractionChannelKeys: ["portal-web"],
                        MinimumInteractionCount: 2,
                        InteractionLookbackDays: 30
                    )
                )
            )
        ).Value;
        Assert.Equal(1, seg.CachedMemberCount);

        var segCache = sp.GetRequiredService<IPersonalizationSegmentCache>();
        Assert.True(await segCache.IsMemberAsync("engaged-members", personId));
        Assert.False(await segCache.IsMemberAsync("engaged-members", Guid.CreateVersion7()));
    }

    private static async Task VerifyAdaptiveSaturationAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        var msg = (
            await commands.SendAsync<CreateAdaptiveMessageCommand, AdaptiveMessageResponse>(
                new CreateAdaptiveMessageCommand("Portal Hero", "portal-hero")
            )
        ).Value;

        _ = await commands.SendAsync<AddAdaptiveMessageAdaptationCommand, AdaptiveMessageResponse>(
            new AddAdaptiveMessageAdaptationCommand(
                msg.Id,
                "Engaged Callout",
                "Thank You for Staying Connected!",
                "Register for the leadership cohort.",
                Order: 0,
                SegmentKeys: ["engaged-members"],
                ViewSaturationCount: 1,
                ViewSaturationInDays: 7
            )
        );
        _ = await commands.SendAsync<AddAdaptiveMessageAdaptationCommand, AdaptiveMessageResponse>(
            new AddAdaptiveMessageAdaptationCommand(
                msg.Id,
                "Default Callout",
                "Welcome to Ruth Community",
                "Discover upcoming opportunities.",
                Order: 10
            )
        );

        var firstResolve = (
            await queries.QueryAsync<ResolveAdaptiveMessageQuery, ResolvedAdaptiveMessageResponse>(
                new ResolveAdaptiveMessageQuery(
                    "portal-hero",
                    personId,
                    RecordViewInteraction: true
                )
            )
        ).Value;
        Assert.Equal("Engaged Callout", firstResolve.SelectedAdaptation?.Name);

        _ = await commands.SendAsync<
            FlushInteractionBufferCommand,
            InteractionBufferStatusResponse
        >(new FlushInteractionBufferCommand());

        var secondResolve = (
            await queries.QueryAsync<ResolveAdaptiveMessageQuery, ResolvedAdaptiveMessageResponse>(
                new ResolveAdaptiveMessageQuery(
                    "portal-hero",
                    personId,
                    RecordViewInteraction: false
                )
            )
        ).Value;
        Assert.Equal("Default Callout", secondResolve.SelectedAdaptation?.Name);
    }

    private static async Task VerifyPersonMergedConsumerAsync(
        IServiceProvider sp,
        Guid retiredPersonId
    )
    {
        var survivingPersonId = Guid.CreateVersion7();
        var consumers = sp.GetServices<IIntegrationEventConsumer<PersonMergedIntegrationEvent>>();
        var evt = new PersonMergedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            survivingPersonId,
            retiredPersonId,
            ExternalRecordGuids: [],
            HadMultipleDistinctEmails: false
        );

        foreach (var consumer in consumers)
        {
            await consumer.ConsumeAsync(evt);
        }

        var db = sp.GetRequiredService<CmsDbContext>();
        var retiredCount = await db
            .Interactions.AsNoTracking()
            .CountAsync(i => i.PersonId == retiredPersonId);
        var survivingCount = await db
            .Interactions.AsNoTracking()
            .CountAsync(i => i.PersonId == survivingPersonId);
        Assert.Equal(0, retiredCount);
        Assert.True(survivingCount >= 2);
    }
}
