using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

[Collection("Postgres18")]
public sealed class OutboxDispatcherIsolationIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class FailingModuleProcessor(string schema, Exception exception)
        : IOutboxModuleProcessor
    {
        public string Schema { get; } = schema;

        public Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<int>(exception);
        }

        public Task<int> PruneProcessedAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(0);
        }
    }

    [Fact]
    public async Task OutboxDispatcherService_Isolates_Failing_Schema_And_Continues_Healthy_Schemas()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var state = new OutboxTestConsumerState();
        await using var provider = BuildProviderWithFailingProcessor(connectionString, state);
        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "IsolatedHealthy");

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            db.EnqueueIntegrationEvent(evt);
            _ = await db.SaveChangesAsync();
        }

        var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        Assert.Equal(1, await dispatcher.ProcessPendingOnceAsync());
        Assert.Equal(evt.EventId, Assert.Single(state.ConsumerAEvents));
    }

    [Fact]
    public async Task OutboxDispatcherService_Rethrows_OperationCanceledException_When_Token_Canceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var dispatcher = new OutboxDispatcherService(
            [new FailingModuleProcessor("canceled", new OperationCanceledException(cts.Token))],
            Options.Create(new OutboxOptions()),
            new SystemClock(TimeProvider.System),
            NullLogger<OutboxDispatcherService>.Instance
        );

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.ProcessPendingOnceAsync(cts.Token)
        );
    }

    private static ServiceProvider BuildProviderWithFailingProcessor(
        string connectionString,
        OutboxTestConsumerState state
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton(state);
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddSingleton<IOutboxModuleProcessor>(
            new FailingModuleProcessor("broken", new InvalidOperationException("Schema offline"))
        );
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            OutboxTestConsumerA,
            AlphaModuleDbContext
        >();
        return services.BuildServiceProvider();
    }
}
