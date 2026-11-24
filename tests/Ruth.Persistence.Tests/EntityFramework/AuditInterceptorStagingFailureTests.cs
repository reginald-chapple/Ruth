using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class AuditInterceptorStagingFailureTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed record UnserializableIntegrationEvent(Action Callback) : IntegrationEvent;

    [Fact]
    public async Task MidStaging_Serialization_Failure_Discards_Previously_Staged_Outbox_Messages()
    {
        var connectionString = await _fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await BuildAndInitializeProviderAsync(connectionString);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        var failing = new AuditedWidget(TestWidgetId.Create(), "Failing", DateTimeOffset.UtcNow);
        failing.AddIntegrationEvent(new TestWidgetIntegrationEvent(failing.Id.Value, "First"));
        failing.AddIntegrationEvent(new UnserializableIntegrationEvent(static () => { }));
        _ = db.AuditedWidgets.Add(failing);

        _ = await Assert.ThrowsAsync<NotSupportedException>(() => db.SaveChangesAsync());
        Assert.Empty(db.ChangeTracker.Entries<OutboxMessage>());

        db.Entry(failing).State = EntityState.Detached;
        _ = db.AuditedWidgets.Add(
            new AuditedWidget(TestWidgetId.Create(), "Unrelated", DateTimeOffset.UtcNow)
        );
        _ = await db.SaveChangesAsync();

        await AssertZeroOutboxMessagesAsync(provider);
    }

    private static async Task<ServiceProvider> BuildAndInitializeProviderAsync(
        string connectionString
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSharedKernel(new FakeTimeProvider(DateTimeOffset.UtcNow));
        _ = services.AddRuthPersistence(connectionString);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connectionString, "alpha");
        var provider = services.BuildServiceProvider();

        await using var setupScope = provider.CreateAsyncScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(setupDb, "alpha");
        return provider;
    }

    private static async Task AssertZeroOutboxMessagesAsync(ServiceProvider provider)
    {
        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
        Assert.Equal(0, await verifyDb.OutboxMessages.CountAsync());
    }
}
