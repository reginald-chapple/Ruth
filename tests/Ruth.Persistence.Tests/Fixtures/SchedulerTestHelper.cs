using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Scheduling;
using Ruth.Persistence.Tests.TestDoubles;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Jobs;

namespace Ruth.Persistence.Tests.Fixtures;

internal static class SchedulerTestHelper
{
    public static ServiceProvider BuildProvider(
        string connectionString,
        string workerId,
        FakeTimeProvider? timeProvider = null,
        Action<SchedulerOptions>? configureOptions = null,
        params IScheduledJob[] jobs
    )
    {
        var services = new ServiceCollection();
        _ = services.AddLogging();
        if (timeProvider is not null)
        {
            _ = services.AddSharedKernel(timeProvider);
        }

        _ = services.AddRuthPersistence(connectionString, configureScheduler: opts =>
        {
            opts.WorkerId = workerId;
            configureOptions?.Invoke(opts);
        });

        var defaultJobDescriptor = services.FirstOrDefault(static d =>
            d.ServiceType == typeof(IScheduledJob)
            && d.ImplementationType == typeof(OutboxRetentionCleanupJob)
        );
        if (defaultJobDescriptor is not null)
        {
            _ = services.Remove(defaultJobDescriptor);
        }

        foreach (var job in jobs)
        {
            _ = services.AddSingleton(job);
        }

        return services.BuildServiceProvider();
    }
}
