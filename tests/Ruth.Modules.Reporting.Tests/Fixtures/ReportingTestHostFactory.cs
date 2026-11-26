using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Communication;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.Modules.Reporting.Infrastructure.Analytics;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Fixtures;

internal static class ReportingTestHostFactory
{
    public static async Task<ServiceProvider> CreateAsync(
        string connectionString,
        IDataViewSourceProvider? extraSourceProvider = null
    )
    {
        AmbientTenantContext.SetTestHarnessDefault(TenantId.Create(), TenantLifecycleStage.Live);

        var provider = BuildServiceProvider(connectionString, extraSourceProvider);
        await InitializeDatabaseAsync(provider);
        return provider;
    }

    public static async Task VerifyPersonMergeRepointsAsync(IServiceProvider sp, Guid retiredId)
    {
        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        var survivingId = Guid.CreateVersion7();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingId,
                retiredId,
                [],
                HadMultipleDistinctEmails: false
            )
        );
        var db = sp.GetRequiredService<ReportingDbContext>();
        Assert.True(await db.DataViewPersistedValues.AnyAsync(v => v.EntityId == survivingId));
        Assert.True(await db.MergeTemplates.AnyAsync(t => t.OwnerPersonId == survivingId));
    }

    private static ServiceProvider BuildServiceProvider(
        string connectionString,
        IDataViewSourceProvider? extraSourceProvider
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "false",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddLogging(static b => b.SetMinimumLevel(LogLevel.Warning));
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddCommunicationModule(configuration);
        _ = services.AddReportingModule(configuration);

        if (extraSourceProvider is not null)
        {
            _ = services.AddSingleton(extraSourceProvider);
        }

        return services.BuildServiceProvider();
    }

    private static async Task InitializeDatabaseAsync(ServiceProvider provider)
    {
        await provider.MigrateModuleSchemaAsync<CommunicationDbContext>();
        await provider.MigrateModuleSchemaAsync<ReportingDbContext>();
        await StarSchemaSqlViews.EnsureBiViewsAsync(
            provider.GetService<PostgresRlsRoleProvisioner>(),
            fallbackDb: null,
            CancellationToken.None
        );
    }
}
