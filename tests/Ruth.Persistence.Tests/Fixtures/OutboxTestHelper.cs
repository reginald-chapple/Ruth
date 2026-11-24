using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Persistence.Tests.Fixtures;

internal static class OutboxTestHelper
{
    public static ServiceProvider BuildProvider(
        string connectionString,
        OutboxTestConsumerState state,
        FakeTimeProvider? fakeTime = null,
        bool includeConsumerB = false,
        Action<OutboxOptions>? configureOutbox = null
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton(state);
        if (fakeTime is not null)
        {
            _ = services.AddSharedKernel(fakeTime);
        }

        _ = services.AddRuthPersistence(connectionString, configureOutbox);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            OutboxTestConsumerA,
            AlphaModuleDbContext
        >();

        if (includeConsumerB)
        {
            _ = services.AddIdempotentIntegrationEventConsumer<
                TestWidgetIntegrationEvent,
                OutboxTestConsumerB,
                AlphaModuleDbContext
            >();
        }

        return services.BuildServiceProvider();
    }
}
