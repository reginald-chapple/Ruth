using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.Primitives;
using Xunit;

namespace Ruth.Persistence.Tests.Outbox;

[Collection("Postgres18")]
public sealed class OutboxAndInboxIntegrationTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    [Fact]
    public async Task Rolled_Back_Transaction_Writes_Zero_Outbox_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            new OutboxTestConsumerState()
        );

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");

            await using var tx = await db.Database.BeginTransactionAsync();
            var widget = new AuditedWidget(
                TestWidgetId.Create(),
                "Transient",
                DateTimeOffset.UtcNow
            );
            _ = db.AuditedWidgets.Add(widget);
            db.EnqueueIntegrationEvent(new TestWidgetIntegrationEvent(widget.Id.Value, "Created"));
            _ = await db.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(0, await verifyDb.AuditedWidgets.CountAsync());
        Assert.Equal(0, await verifyDb.OutboxMessages.CountAsync());
    }

    [Fact]
    public async Task Concurrent_Processors_Claim_Disjoint_Batches_With_Zero_Duplicates()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var state = new OutboxTestConsumerState();
        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            state,
            configureOutbox: opts => opts.BatchSize = 5
        );

        const int totalMessages = 20;
        await SeedBatchMessagesAsync(provider, totalMessages);

        var processor = provider.GetRequiredService<IOutboxModuleProcessor>();
        var workerTasks = Enumerable
            .Range(0, 4)
            .Select(async _ =>
            {
                var count = 0;
                while (await processor.ProcessBatchAsync() > 0)
                {
                    count++;
                }

                return count;
            });

        _ = await Task.WhenAll(workerTasks);

        Assert.Equal(totalMessages, state.ConsumerAEvents.Count);
        Assert.Equal(totalMessages, state.ConsumerAEvents.Distinct().Count());

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(
            totalMessages,
            await verifyDb.OutboxMessages.CountAsync(x =>
                x.ProcessedOnUtc != null && x.LockedUntilUtc == null
            )
        );
    }

    [Fact]
    public async Task Partial_Consumer_Failure_Retries_Only_Failed_Consumer_Via_Inbox_Deduplication()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var initialUtc = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(initialUtc);
        var state = new OutboxTestConsumerState { FailConsumerBOnce = true };

        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            state,
            fakeTime,
            includeConsumerB: true,
            opts =>
            {
                opts.BaseRetryDelay = TimeSpan.FromSeconds(10);
                opts.MaxRetryDelay = TimeSpan.FromMinutes(1);
                opts.MaxRetries = 3;
            }
        );

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "PartialFail");
        await EnqueueSingleEventAsync(provider, evt);

        var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        _ = await dispatcher.ProcessPendingOnceAsync();

        _ = Assert.Single(state.ConsumerAEvents);
        Assert.Empty(state.ConsumerBEvents);
        Assert.Equal(1, state.ConsumerBAttempts);
        await AssertScheduledRetryAsync(provider, evt.EventId, initialUtc.AddSeconds(10));

        fakeTime.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(0, await dispatcher.ProcessPendingOnceAsync());

        fakeTime.Advance(TimeSpan.FromSeconds(6));
        Assert.Equal(1, await dispatcher.ProcessPendingOnceAsync());

        await AssertBothConsumersCompletedAsync(provider, evt.EventId, state);
    }

    [Fact]
    public async Task Unknown_EventType_And_Exhausted_Retries_DeadLetter_Without_Blocking_Healthy_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero));
        var state = new OutboxTestConsumerState { AlwaysFailConsumerA = true };

        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            state,
            fakeTime,
            includeConsumerB: false,
            opts =>
            {
                opts.MaxRetries = 2;
                opts.BaseRetryDelay = TimeSpan.FromSeconds(1);
            }
        );

        var badTypeId = StronglyTypedId.NextGuid();
        var exhaustedId = StronglyTypedId.NextGuid();
        await SeedDeadLetterCandidatesAsync(provider, fakeTime.GetUtcNow(), badTypeId, exhaustedId);

        var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        _ = await dispatcher.ProcessPendingOnceAsync();
        fakeTime.Advance(TimeSpan.FromSeconds(5));
        _ = await dispatcher.ProcessPendingOnceAsync();

        await AssertDeadLetteredRowsAsync(provider, badTypeId, exhaustedId);
    }

    [Fact]
    public async Task EnqueueIntegrationEvent_Uses_Custom_Logical_Key_Registered_In_Application_DI()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var state = new OutboxTestConsumerState();
        await using var provider = OutboxTestHelper.BuildProvider(connectionString, state);

        var registry = provider.GetRequiredService<OutboxEventTypeRegistry>();
        _ = registry.Register<TestWidgetIntegrationEvent>("custom.sample.event");

        var evt = new TestWidgetIntegrationEvent(StronglyTypedId.NextGuid(), "CustomKey");
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            db.EnqueueIntegrationEvent(evt);
            _ = await db.SaveChangesAsync();

            var stored = await db.OutboxMessages.SingleAsync(x => x.Id == evt.EventId);
            Assert.Equal("custom.sample.event", stored.EventType);
        }

        var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        Assert.Equal(1, await dispatcher.ProcessPendingOnceAsync());

        var deliveredId = Assert.Single(state.ConsumerAEvents);
        Assert.Equal(evt.EventId, deliveredId);
    }

    [Fact]
    public async Task Malformed_EventType_PoisonPills_DeadLetter_Without_Blocking_Healthy_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 4, 3, 0, 0, 0, TimeSpan.Zero));
        var state = new OutboxTestConsumerState();
        await using var provider = OutboxTestHelper.BuildProvider(
            connectionString,
            state,
            fakeTime
        );

        var poison1 = StronglyTypedId.NextGuid();
        var poison2 = StronglyTypedId.NextGuid();
        var poison3 = StronglyTypedId.NextGuid();
        var healthy = new TestWidgetIntegrationEvent(
            StronglyTypedId.NextGuid(),
            "HealthyAfterPoison"
        );
        var now = fakeTime.GetUtcNow();

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            _ = db.OutboxMessages.Add(
                OutboxMessage.Create(poison1, "Foo, Version=notaversion", "{}", now)
            );
            _ = db.OutboxMessages.Add(
                OutboxMessage.Create(poison2, "Foo, PublicKeyToken=nothex", "{}", now.AddSeconds(1))
            );
            _ = db.OutboxMessages.Add(
                OutboxMessage.Create(poison3, "A, B, C, D", "{}", now.AddSeconds(2))
            );
            db.EnqueueIntegrationEvent(healthy);
            _ = await db.SaveChangesAsync();
        }

        var dispatcher = provider.GetRequiredService<OutboxDispatcherService>();
        Assert.Equal(4, await dispatcher.ProcessPendingOnceAsync());

        var deliveredId = Assert.Single(state.ConsumerAEvents);
        Assert.Equal(healthy.EventId, deliveredId);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var poisonedRows = await verifyDb
            .OutboxMessages.Where(x => x.Id == poison1 || x.Id == poison2 || x.Id == poison3)
            .ToListAsync();
        Assert.Equal(3, poisonedRows.Count);
        Assert.All(poisonedRows, row =>
        {
            _ = Assert.NotNull(row.DeadLetteredOnUtc);
            Assert.Null(row.ProcessedOnUtc);
        });
    }

    private static async Task SeedBatchMessagesAsync(ServiceProvider provider, int count)
    {
        await using var setupScope = provider.CreateAsyncScope();
        var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");

        for (var i = 0; i < count; i++)
        {
            db.EnqueueIntegrationEvent(
                new TestWidgetIntegrationEvent(
                    StronglyTypedId.NextGuid(),
                    $"Msg-{i.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                )
            );
        }

        _ = await db.SaveChangesAsync();
    }

    private static async Task EnqueueSingleEventAsync(
        ServiceProvider provider,
        TestWidgetIntegrationEvent evt
    )
    {
        await using var setupScope = provider.CreateAsyncScope();
        var db = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
        db.EnqueueIntegrationEvent(evt);
        _ = await db.SaveChangesAsync();
    }

    private static async Task AssertScheduledRetryAsync(
        ServiceProvider provider,
        Guid eventId,
        DateTimeOffset expectedRetryAt
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var msg = await db.OutboxMessages.SingleAsync(x => x.Id == eventId);
        Assert.Null(msg.ProcessedOnUtc);
        Assert.Equal(1, msg.RetryCount);
        Assert.Equal(expectedRetryAt, msg.NextRetryOnUtc);
        Assert.Contains("TimeoutException", msg.Error, StringComparison.Ordinal);
    }

    private static async Task AssertBothConsumersCompletedAsync(
        ServiceProvider provider,
        Guid eventId,
        OutboxTestConsumerState state
    )
    {
        _ = Assert.Single(state.ConsumerAEvents);
        _ = Assert.Single(state.ConsumerBEvents);
        Assert.Equal(2, state.ConsumerBAttempts);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var completed = await db.OutboxMessages.SingleAsync(x => x.Id == eventId);
        _ = Assert.NotNull(completed.ProcessedOnUtc);
        Assert.Null(completed.LockedUntilUtc);
        Assert.Null(completed.LockToken);
        Assert.Null(completed.Error);

        var inboxEntries = await db.InboxMessages.Where(x => x.MessageId == eventId).ToListAsync();
        Assert.Equal(2, inboxEntries.Count);
    }

    private static async Task SeedDeadLetterCandidatesAsync(
        ServiceProvider provider,
        DateTimeOffset now,
        Guid badTypeId,
        Guid exhaustedId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");

        _ = db.OutboxMessages.Add(
            OutboxMessage.Create(badTypeId, "Unknown.NonExistent.IntegrationEvent", "{}", now)
        );
        _ = db.OutboxMessages.Add(
            OutboxMessage.Create(
                exhaustedId,
                OutboxEventTypeRegistry.GetCanonicalTypeName(typeof(TestWidgetIntegrationEvent)),
                $$"""{"widgetId":"{{Guid.NewGuid()}}","action":"Fail","eventId":"{{exhaustedId}}","occurredOnUtc":"2026-04-02T00:00:00Z"}""",
                now.AddSeconds(1)
            )
        );
        _ = await db.SaveChangesAsync();
    }

    private static async Task AssertDeadLetteredRowsAsync(
        ServiceProvider provider,
        Guid badTypeId,
        Guid exhaustedId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();

        var badTypeRow = await db.OutboxMessages.SingleAsync(x => x.Id == badTypeId);
        _ = Assert.NotNull(badTypeRow.DeadLetteredOnUtc);
        Assert.Null(badTypeRow.ProcessedOnUtc);
        Assert.Contains(
            "Unknown integration event type",
            badTypeRow.Error,
            StringComparison.Ordinal
        );

        var exhaustedRow = await db.OutboxMessages.SingleAsync(x => x.Id == exhaustedId);
        _ = Assert.NotNull(exhaustedRow.DeadLetteredOnUtc);
        Assert.Null(exhaustedRow.ProcessedOnUtc);
        Assert.Equal(2, exhaustedRow.RetryCount);
    }
}
