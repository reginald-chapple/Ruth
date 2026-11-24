using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

[Collection("Postgres18")]
public sealed class OutboxBackgroundAndRaceIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task OutboxDispatcherService_Background_Loop_Delivers_Pending_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var state = new OutboxTestConsumerState();
        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            state,
            configureOutbox: opts => opts.PollingInterval = TimeSpan.FromMilliseconds(25)
        );

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "HostedSweep");
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            db.EnqueueIntegrationEvent(evt);
            _ = await db.SaveChangesAsync();
        }

        var hosted = provider.GetRequiredService<OutboxDispatcherService>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await hosted.StartAsync(cts.Token);

        for (var i = 0; i < 40 && state.ConsumerAEvents.IsEmpty; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25), cts.Token);
        }

        await hosted.StopAsync(CancellationToken.None);
        _ = Assert.Single(state.ConsumerAEvents);
    }

    [Fact]
    public async Task IdempotentConsumer_ConcurrentSameEventDispatch_ExecutesInnerConsumerExactlyOnce()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var state = new OutboxTestConsumerState();
        await using var provider = OutboxTestHelper.BuildProvider(connectionString, state);

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "ConcurrentRace");
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
        }

        const int concurrency = 10;
        var startGate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var invocationCount = 0;

        var tasks = Enumerable
            .Range(0, concurrency)
            .Select(async _ =>
            {
                await using var scope = provider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
                var inner = new SlowCountingConsumer(() => Interlocked.Increment(ref invocationCount));
                var wrapper = new IdempotentIntegrationEventConsumer<
                    TestWidgetIntegrationEvent,
                    SlowCountingConsumer,
                    AlphaModuleDbContext
                >(
                    inner,
                    db,
                    scope.ServiceProvider.GetRequiredService<IClock>(),
                    scope.ServiceProvider.GetRequiredService<
                        ILogger<
                            IdempotentIntegrationEventConsumer<
                                TestWidgetIntegrationEvent,
                                SlowCountingConsumer,
                                AlphaModuleDbContext
                            >
                        >
                    >()
                );

                await startGate.Task;
                await wrapper.ConsumeAsync(evt);
            })
            .ToArray();

        _ = startGate.TrySetResult();
        await Task.WhenAll(tasks);

        Assert.Equal(1, Volatile.Read(ref invocationCount));

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(1, await verifyDb.InboxMessages.CountAsync(x => x.MessageId == evt.EventId));
    }

    private sealed class SlowCountingConsumer(Action onInvoked)
        : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public async Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            onInvoked();
            await Task.Delay(TimeSpan.FromMilliseconds(30), cancellationToken);
        }
    }
}
