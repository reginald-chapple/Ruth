using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tests.TestDoubles;
using Xunit;

namespace Ruth.SharedKernel.Tests.Events;

public sealed class EventDispatchingTests
{
    private sealed record SampleDomainEvent(string Name, DateTimeOffset OccurredAt)
        : DomainEvent(OccurredAt);

    private sealed record DefaultTimestampDomainEvent(string Name) : DomainEvent;

    private sealed record SampleIntegrationEvent(string Payload, DateTimeOffset OccurredAt)
        : IntegrationEvent(OccurredAt);

    private sealed record DefaultTimestampIntegrationEvent(string Payload) : IntegrationEvent;

    private sealed class EventSink
    {
        public List<string> Received { get; } = [];
    }

    private sealed class FirstDomainConsumer(EventSink sink)
        : IDomainEventConsumer<SampleDomainEvent>
    {
        public Task ConsumeAsync(
            SampleDomainEvent domainEvent,
            CancellationToken cancellationToken = default
        )
        {
            sink.Received.Add($"domain-1:{domainEvent.Name}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondDomainConsumer(EventSink sink)
        : IDomainEventConsumer<SampleDomainEvent>
    {
        public Task ConsumeAsync(
            SampleDomainEvent domainEvent,
            CancellationToken cancellationToken = default
        )
        {
            sink.Received.Add($"domain-2:{domainEvent.Name}");
            return Task.CompletedTask;
        }
    }

    private sealed class FirstIntegrationConsumer(EventSink sink)
        : IIntegrationEventConsumer<SampleIntegrationEvent>
    {
        public Task ConsumeAsync(
            SampleIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            sink.Received.Add($"integration-1:{integrationEvent.Payload}");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondIntegrationConsumer(EventSink sink)
        : IIntegrationEventConsumer<SampleIntegrationEvent>
    {
        public Task ConsumeAsync(
            SampleIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            sink.Received.Add($"integration-2:{integrationEvent.Payload}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Domain_And_Integration_Events_Assign_UuidV7_And_Utc_Timestamps()
    {
        var offsetTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.FromHours(-5));

        var domainEvent = new SampleDomainEvent("Created", offsetTime);
        var defaultDomain = new DefaultTimestampDomainEvent("Default");
        var integrationEvent = new SampleIntegrationEvent("Merged", offsetTime);
        var defaultIntegration = new DefaultTimestampIntegrationEvent("Default");

        Assert.Equal(7, domainEvent.EventId.Version);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOnUtc.Offset);
        Assert.Equal(17, domainEvent.OccurredOnUtc.Hour);
        Assert.Equal(7, defaultDomain.EventId.Version);

        Assert.Equal(7, integrationEvent.EventId.Version);
        Assert.Equal(TimeSpan.Zero, integrationEvent.OccurredOnUtc.Offset);
        Assert.Equal(17, integrationEvent.OccurredOnUtc.Hour);
        Assert.Equal(7, defaultIntegration.EventId.Version);
    }

    [Fact]
    public async Task DomainEventDispatcher_Dispatches_Typed_And_Polymorphic_Events()
    {
        var sink = new EventSink();
        var logger = new RecordingLogger<DomainEventDispatcher>();
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(sink)
            .AddSingleton<ILogger<DomainEventDispatcher>>(logger)
            .AddSharedKernel()
            .AddDomainEventConsumer<SampleDomainEvent, FirstDomainConsumer>()
            .AddDomainEventConsumer<SampleDomainEvent, SecondDomainConsumer>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();

        var timestamp = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var first = new SampleDomainEvent("Typed", timestamp);
        var second = new SampleDomainEvent("Poly", timestamp);

        await dispatcher.DispatchAsync(first);
        await dispatcher.DispatchAsync([second, second]);

        Assert.Equal(
            [
                "domain-1:Typed",
                "domain-2:Typed",
                "domain-1:Poly",
                "domain-2:Poly",
                "domain-1:Poly",
                "domain-2:Poly",
            ],
            sink.Received
        );
        Assert.Equal(3, logger.Entries.Count);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync<SampleDomainEvent>(null!)
        );
        IEnumerable<IDomainEvent>? nullEvents = null;
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync(nullEvents!)
        );
        _ = await Assert.ThrowsAsync<ArgumentException>(() => dispatcher.DispatchAsync([null!]));
    }

    [Fact]
    public async Task IntegrationEventDispatcher_Dispatches_Typed_And_Polymorphic_Events()
    {
        var sink = new EventSink();
        var logger = new RecordingLogger<IntegrationEventDispatcher>();
        var services = new ServiceCollection();
        _ = services
            .AddSingleton(sink)
            .AddSingleton<ILogger<IntegrationEventDispatcher>>(logger)
            .AddSharedKernel()
            .AddIntegrationEventConsumer<SampleIntegrationEvent, FirstIntegrationConsumer>()
            .AddIntegrationEventConsumer<SampleIntegrationEvent, SecondIntegrationConsumer>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        var timestamp = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var typedEvent = new SampleIntegrationEvent("Typed", timestamp);
        IIntegrationEvent polyEvent = new SampleIntegrationEvent("Poly", timestamp);

        await dispatcher.DispatchAsync(typedEvent);
        await dispatcher.DispatchAsync(polyEvent);

        Assert.Equal(
            [
                "integration-1:Typed",
                "integration-2:Typed",
                "integration-1:Poly",
                "integration-2:Poly",
            ],
            sink.Received
        );
        Assert.Equal(2, logger.Entries.Count);

        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync<SampleIntegrationEvent>(null!)
        );
        IIntegrationEvent? nullIntegrationEvent = null;
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync(nullIntegrationEvent!)
        );
    }
}
