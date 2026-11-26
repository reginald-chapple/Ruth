using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Scheduling;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Persistence.DependencyInjection;

internal static class PersistenceRegistrationHelper
{
    public static IServiceCollection AddRuthPersistenceCore(
        IServiceCollection services,
        string connectionString,
        IConfiguration? configuration,
        Action<OutboxOptions>? configureOutbox,
        Action<SchedulerOptions>? configureScheduler
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _ = services.AddSharedKernel();
        RegisterPersistenceOptions(services, configuration, configureOutbox, configureScheduler);

        services.TryAddSingleton(sp => new PostgresRlsRoleProvisioner(
            connectionString,
            sp.GetRequiredService<IOptions<RlsOptions>>()
        ));
        services.TryAddSingleton<TenantRlsConnectionInterceptor>();
        services.TryAddSingleton<TenantRlsCommandInterceptor>();
        services.TryAddSingleton<TenantRlsTransactionInterceptor>();
        services.TryAddSingleton<OutboxEventTypeRegistry>();
        services.TryAddScoped<AuditAndConcurrencySaveChangesInterceptor>();

        RegisterSchedulerServices(services, connectionString);
        RegisterOutboxServices(services);

        return services;
    }

    public static void RegisterKeyedDataSource<TContext>(
        IServiceCollection services,
        string connectionString,
        ModuleDbContextRegistrationOptions regOptions
    )
        where TContext : ModuleDbContext
    {
        if (regOptions.ConfigureDataSource is null)
        {
            return;
        }

        _ = services.AddKeyedSingleton(typeof(TContext), (serviceProvider, _) =>
        {
            var effectiveConnectionString = ResolveRuntimeConnectionString(
                serviceProvider,
                connectionString,
                regOptions
            );
            var builder = new NpgsqlDataSourceBuilder(effectiveConnectionString);
            regOptions.ConfigureDataSource(builder);
            return builder.Build();
        });
    }

    public static void RegisterDbContextOptions<TContext>(
        IServiceCollection services,
        string connectionString,
        string validatedSchema,
        ModuleDbContextRegistrationOptions regOptions
    )
        where TContext : ModuleDbContext
    {
        services.TryAddSingleton(
            new ModuleMigrationDescriptor<TContext>(connectionString, validatedSchema, regOptions)
        );

        _ = services.AddDbContext<TContext>(
            (serviceProvider, options) =>
            {
                if (regOptions.ConfigureDataSource is not null)
                {
                    var dataSource = serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(
                        typeof(TContext)
                    );
                    _ = options.UseNpgsql(dataSource, npgsql =>
                        ConfigureNpgsqlOptions(npgsql, validatedSchema, regOptions)
                    );
                }
                else
                {
                    var effectiveConnectionString = ResolveRuntimeConnectionString(
                        serviceProvider,
                        connectionString,
                        regOptions
                    );
                    _ = options.UseNpgsql(effectiveConnectionString, npgsql =>
                        ConfigureNpgsqlOptions(npgsql, validatedSchema, regOptions)
                    );
                }

                AddModuleInterceptors(serviceProvider, options, regOptions.IsTenantScopedSchema);
                regOptions.ConfigureDbContext?.Invoke(options);
            }
        );
    }

    public static void RegisterOutboxProcessor<TContext>(
        IServiceCollection services,
        string validatedSchema,
        bool isTenantScopedSchema
    )
        where TContext : ModuleDbContext
    {
        services.TryAddSingleton(
            new OutboxSchemaDescriptor<TContext>(validatedSchema, isTenantScopedSchema)
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOutboxModuleProcessor, OutboxModuleProcessor<TContext>>()
        );
    }

    private static string ResolveRuntimeConnectionString(
        IServiceProvider serviceProvider,
        string connectionString,
        ModuleDbContextRegistrationOptions regOptions
    )
    {
        if (!regOptions.IsTenantScopedSchema)
        {
            return connectionString;
        }

        var provisioner = serviceProvider.GetService<PostgresRlsRoleProvisioner>();
        return provisioner is not null
            ? provisioner.BuildTenantRuntimeConnectionString(connectionString)
            : connectionString;
    }

    private static void AddModuleInterceptors(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder options,
        bool isTenantScopedSchema
    )
    {
        if (isTenantScopedSchema)
        {
            _ = options.AddInterceptors(
                serviceProvider.GetRequiredService<TenantRlsConnectionInterceptor>(),
                serviceProvider.GetRequiredService<TenantRlsTransactionInterceptor>(),
                serviceProvider.GetRequiredService<TenantRlsCommandInterceptor>(),
                serviceProvider.GetRequiredService<AuditAndConcurrencySaveChangesInterceptor>()
            );
            return;
        }

        _ = options.AddInterceptors(
            serviceProvider.GetRequiredService<AuditAndConcurrencySaveChangesInterceptor>()
        );
    }

    private static void RegisterPersistenceOptions(
        IServiceCollection services,
        IConfiguration? configuration,
        Action<OutboxOptions>? configureOutbox,
        Action<SchedulerOptions>? configureScheduler
    )
    {
        var outboxBuilder = services.AddOptions<OutboxOptions>();
        var schedulerBuilder = services.AddOptions<SchedulerOptions>();
        var rlsBuilder = services.AddOptions<RlsOptions>();

        if (configuration is not null)
        {
            _ = outboxBuilder.Bind(configuration.GetSection("Outbox"));
            _ = schedulerBuilder.Bind(configuration.GetSection("Scheduler"));
            _ = rlsBuilder.Bind(configuration.GetSection("Persistence:Rls"));
        }
        else
        {
            _ = outboxBuilder.Configure<IServiceProvider>(
                static (options, sp) =>
                    sp.GetService<IConfiguration>()?.GetSection("Outbox").Bind(options)
            );
            _ = schedulerBuilder.Configure<IServiceProvider>(
                static (options, sp) =>
                    sp.GetService<IConfiguration>()?.GetSection("Scheduler").Bind(options)
            );
            _ = rlsBuilder.Configure<IServiceProvider>(
                static (options, sp) =>
                    sp.GetService<IConfiguration>()?.GetSection("Persistence:Rls").Bind(options)
            );
        }

        if (configureOutbox is not null)
        {
            _ = services.Configure(configureOutbox);
        }

        if (configureScheduler is not null)
        {
            _ = services.Configure(configureScheduler);
        }
    }

    private static void RegisterSchedulerServices(
        IServiceCollection services,
        string connectionString
    )
    {
        _ = services.AddDbContext<SchedulerDbContext>(
            (serviceProvider, options) =>
            {
                _ = options.UseNpgsql(connectionString, static npgsql =>
                    npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        SchedulerDbContext.SchemaName
                    )
                );
                _ = options.AddInterceptors(
                    serviceProvider.GetRequiredService<AuditAndConcurrencySaveChangesInterceptor>()
                );
            }
        );

        services.TryAddSingleton(serviceProvider => new PostgresAdvisoryLockProvider(
            connectionString,
            serviceProvider.GetRequiredService<IOptions<SchedulerOptions>>(),
            serviceProvider.GetRequiredService<ILogger<PostgresAdvisoryLockProvider>>()
        ));
        services.TryAddSingleton<ScheduledJobExecutor>();
        services.TryAddSingleton<ScheduledJobEngine>();
        _ = services.AddScheduledJob<OutboxRetentionCleanupJob>();

        _ = services.AddHostedService(static sp => new SchedulerBootstrapHostedService(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ILogger<SchedulerBootstrapHostedService>>(),
            sp.GetService<IConfiguration>()
        ));
        _ = services.AddHostedService(static sp => sp.GetRequiredService<ScheduledJobEngine>());
    }

    private static void RegisterOutboxServices(IServiceCollection services)
    {
        services.TryAddSingleton<OutboxDispatcherService>();
        _ = services.AddHostedService(static sp =>
            sp.GetRequiredService<OutboxDispatcherService>()
        );
    }

    private static void ConfigureNpgsqlOptions(
        NpgsqlDbContextOptionsBuilder npgsql,
        string validatedSchema,
        ModuleDbContextRegistrationOptions regOptions
    )
    {
        _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", validatedSchema);
        regOptions.ConfigureNpgsql?.Invoke(npgsql);
    }
}
