using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Scheduling;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Time;

namespace Ruth.Persistence.DependencyInjection;

/// <summary>Dependency injection registration extensions for Ruth.Persistence.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers shared persistence infrastructure using a named connection string from <paramref name="configuration" />.
    /// </summary>
    public static IServiceCollection AddRuthPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "RuthDatabase",
        Action<OutboxOptions>? configureOutbox = null,
        Action<SchedulerOptions>? configureScheduler = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionStringName);

        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found."
            );

        return PersistenceRegistrationHelper.AddRuthPersistenceCore(
            services,
            connectionString,
            configuration,
            configureOutbox,
            configureScheduler
        );
    }

    /// <summary>
    /// Registers <see cref="AuditAndConcurrencySaveChangesInterceptor" />, Outbox/Inbox dispatcher services,
    /// <see cref="SchedulerDbContext" />, and the PostgreSQL advisory-locked <see cref="ScheduledJobEngine" />.
    /// </summary>
    public static IServiceCollection AddRuthPersistence(
        this IServiceCollection services,
        string connectionString,
        Action<OutboxOptions>? configureOutbox = null,
        Action<SchedulerOptions>? configureScheduler = null
    )
    {
        return PersistenceRegistrationHelper.AddRuthPersistenceCore(
            services,
            connectionString,
            configuration: null,
            configureOutbox,
            configureScheduler
        );
    }

    /// <summary>
    /// Registers a module <see cref="ModuleDbContext" /> with schema-scoped migrations history,
    /// <see cref="AuditAndConcurrencySaveChangesInterceptor" />, Outbox processor discovery,
    /// and optional PostgreSQL 18 extension hooks.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string schema,
        Action<ModuleDbContextRegistrationOptions>? configure = null
    )
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var validatedSchema = SchemaValidator.Validate(schema);

        var regOptions = new ModuleDbContextRegistrationOptions();
        configure?.Invoke(regOptions);

        PersistenceRegistrationHelper.RegisterKeyedDataSource<TContext>(
            services,
            connectionString,
            regOptions
        );
        PersistenceRegistrationHelper.RegisterDbContextOptions<TContext>(
            services,
            connectionString,
            validatedSchema,
            regOptions
        );
        PersistenceRegistrationHelper.RegisterOutboxProcessor<TContext>(
            services,
            validatedSchema,
            regOptions.IsTenantScopedSchema
        );

        return services;
    }

    /// <summary>
    /// Registers an <see cref="IIntegrationEventConsumer{TEvent}" /> wrapped with
    /// <see cref="IdempotentIntegrationEventConsumer{TEvent, TConsumer, TDbContext}" /> deduplication.
    /// </summary>
    public static IServiceCollection AddIdempotentIntegrationEventConsumer<
        TEvent,
        TConsumer,
        TDbContext
    >(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
        where TDbContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(TConsumer), typeof(TConsumer), lifetime));
        services.Add(
            new ServiceDescriptor(
                typeof(IIntegrationEventConsumer<TEvent>),
                static serviceProvider => new IdempotentIntegrationEventConsumer<
                    TEvent,
                    TConsumer,
                    TDbContext
                >(
                    serviceProvider.GetRequiredService<TConsumer>(),
                    serviceProvider.GetRequiredService<TDbContext>(),
                    serviceProvider.GetRequiredService<IClock>(),
                    serviceProvider.GetRequiredService<
                        ILogger<IdempotentIntegrationEventConsumer<TEvent, TConsumer, TDbContext>>
                    >()
                ),
                lifetime
            )
        );

        return services;
    }

    /// <summary>Registers an <see cref="IScheduledJob" /> implementation for execution by <see cref="ScheduledJobEngine" />.</summary>
    public static IServiceCollection AddScheduledJob<TJob>(this IServiceCollection services)
        where TJob : class, IScheduledJob
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IScheduledJob, TJob>());
        return services;
    }
}
