using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

[Collection("Postgres18")]
public sealed class MultiTenantSchedulerAndOutboxFairShareTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class StateAwareRegistry(params TenantExecutionState[] states)
        : IActiveTenantRegistry
    {
        public ValueTask<IReadOnlyList<TenantId>> GetActiveTenantIdsAsync(
            CancellationToken cancellationToken = default
        )
        {
            TenantId[] ids = [.. states.Select(static s => s.TenantId)];
            return ValueTask.FromResult<IReadOnlyList<TenantId>>(ids);
        }

        public ValueTask<IReadOnlyList<TenantExecutionState>> GetActiveTenantsAsync(
            CancellationToken cancellationToken = default
        )
        {
            return ValueTask.FromResult<IReadOnlyList<TenantExecutionState>>(states);
        }
    }

    private sealed class LifecycleRecordingJob(
        TenantId throwingTenant,
        List<(TenantId TenantId, string? Slug, TenantLifecycleStage Stage)> executions
    ) : IScheduledJob
    {
        public string JobKey => "ruth.test.lifecycle-job";

        public string DefaultCronExpression => "*/5 * * * *";

        public Task<JobExecutionResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var ctx = AmbientTenantContext.Instance;
            executions.Add((ctx.TenantId, ctx.TenantSlug, ctx.LifecycleStage));
            return ctx.TenantId == throwingTenant
                ? throw new InvalidOperationException("Simulated tenant failure")
                : Task.FromResult(JobExecutionResult.Success("ok"));
        }
    }

    private sealed class LifecycleRecordingConsumer(
        List<(TenantId TenantId, string? Slug, TenantLifecycleStage Stage)> consumed,
        ITenantContext tenantContext
    ) : IIntegrationEventConsumer<TestWidgetIntegrationEvent>
    {
        public Task ConsumeAsync(
            TestWidgetIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default
        )
        {
            consumed.Add(
                (tenantContext.TenantId, tenantContext.TenantSlug, tenantContext.LifecycleStage)
            );
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Scheduler_And_Outbox_Isolate_PerTenant_Exceptions_And_Preserve_Sandbox_LifecycleStage()
    {
        var connStr = await _fixture.CreateIsolatedDatabaseAsync();
        var sandboxTenant = new TenantExecutionState(
            TenantId.Create(),
            "sandbox-org",
            TenantLifecycleStage.Sandbox
        );
        var liveTenant = new TenantExecutionState(
            TenantId.Create(),
            "live-org",
            TenantLifecycleStage.Live
        );

        var jobExecutions =
            new List<(TenantId TenantId, string? Slug, TenantLifecycleStage Stage)>();
        var outboxConsumed =
            new List<(TenantId TenantId, string? Slug, TenantLifecycleStage Stage)>();
        var job = new LifecycleRecordingJob(sandboxTenant.TenantId, jobExecutions);

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddSingleton(outboxConsumed);
        _ = services.AddSingleton<IActiveTenantRegistry>(
            new StateAwareRegistry(sandboxTenant, liveTenant)
        );
        _ = services.AddRuthPersistence(connStr);
        _ = services.AddModuleDbContext<AlphaModuleDbContext>(connStr, "alpha");
        _ = services.AddIdempotentIntegrationEventConsumer<
            TestWidgetIntegrationEvent,
            LifecycleRecordingConsumer,
            AlphaModuleDbContext
        >();
        await using var provider = services.BuildServiceProvider();

        await SeedOutboxAndSchedulerAsync(provider, sandboxTenant, liveTenant);

        using (AmbientTenantContext.BeginUnresolvedScope())
        {
            var executor = provider.GetRequiredService<ScheduledJobExecutor>();
            Assert.True(await executor.ExecuteAsync(job));
            Assert.Contains(
                (sandboxTenant.TenantId, "sandbox-org", TenantLifecycleStage.Sandbox),
                jobExecutions
            );
            Assert.Contains(
                (liveTenant.TenantId, "live-org", TenantLifecycleStage.Live),
                jobExecutions
            );

            var outbox = provider.GetRequiredService<OutboxDispatcherService>();
            Assert.Equal(2, await outbox.ProcessPendingOnceAsync());
            Assert.Contains(
                (sandboxTenant.TenantId, "sandbox-org", TenantLifecycleStage.Sandbox),
                outboxConsumed
            );
            Assert.Contains(
                (liveTenant.TenantId, "live-org", TenantLifecycleStage.Live),
                outboxConsumed
            );
        }
    }

    private static async Task SeedOutboxAndSchedulerAsync(
        ServiceProvider provider,
        TenantExecutionState sandboxTenant,
        TenantExecutionState liveTenant
    )
    {
        await using (var schedScope = provider.CreateAsyncScope())
        {
            var schedDb = schedScope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(
                schedDb,
                SchedulerDbContext.SchemaName
            );
        }

        using (
            AmbientTenantContext.BeginScope(
                sandboxTenant.TenantId,
                sandboxTenant.TenantSlug,
                sandboxTenant.LifecycleStage
            )
        )
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "alpha");
            db.EnqueueIntegrationEvent(new TestWidgetIntegrationEvent(Guid.CreateVersion7(), "S"));
            _ = await db.SaveChangesAsync();
        }

        using (
            AmbientTenantContext.BeginScope(
                liveTenant.TenantId,
                liveTenant.TenantSlug,
                liveTenant.LifecycleStage
            )
        )
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AlphaModuleDbContext>();
            db.EnqueueIntegrationEvent(new TestWidgetIntegrationEvent(Guid.CreateVersion7(), "L"));
            _ = await db.SaveChangesAsync();
        }
    }
}
