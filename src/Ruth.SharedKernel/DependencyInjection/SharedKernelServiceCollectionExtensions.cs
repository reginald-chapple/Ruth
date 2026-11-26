using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.SharedKernel.DependencyInjection;

/// <summary>Explicit, zero-magic DI registration helpers for <see cref="SharedKernelAssembly" />.</summary>
public static class SharedKernelServiceCollectionExtensions
{
    /// <summary>Registers core clock, ambient tenant context, CQRS dispatchers, and event dispatchers.</summary>
    public static IServiceCollection AddSharedKernel(
        this IServiceCollection services,
        TimeProvider? timeProvider = null,
        TimeZoneInfo? localTimeZone = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);

        var resolvedProvider = timeProvider ?? TimeProvider.System;
        services.TryAddSingleton(resolvedProvider);
        services.TryAddSingleton<IClock>(serviceProvider => new SystemClock(
            serviceProvider.GetRequiredService<TimeProvider>(),
            localTimeZone
        ));
        services.TryAddSingleton<ITenantContext>(AmbientTenantContext.Instance);
        services.TryAddScoped<ICommandDispatcher, CommandDispatcher>();
        services.TryAddScoped<IQueryDispatcher, QueryDispatcher>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.TryAddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();

        return services;
    }

    /// <summary>
    /// Explicitly registers a command handler wrapped in the tenant-guard, validation, and logging decorator pipeline.
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(
            new ServiceDescriptor(
                typeof(ICommandHandler<TCommand>),
                static serviceProvider =>
                {
                    var inner = serviceProvider.GetRequiredService<THandler>();
                    var validators = serviceProvider.GetServices<IRequestValidator<TCommand>>();
                    var validating = new ValidatingCommandHandler<TCommand>(inner, validators);
                    var guarded = new TenantGuardCommandHandler<TCommand>(
                        validating,
                        serviceProvider.GetRequiredService<ITenantContext>(),
                        serviceProvider.GetService<IPrincipalTenantValidator>()
                    );
                    var logger = serviceProvider.GetRequiredService<
                        ILogger<LoggingCommandHandler<TCommand>>
                    >();
                    var clock = serviceProvider.GetRequiredService<IClock>();

                    return new LoggingCommandHandler<TCommand>(guarded, logger, clock);
                },
                lifetime
            )
        );

        return services;
    }

    /// <summary>
    /// Explicitly registers a command handler returning <typeparamref name="TResponse" /> wrapped in the tenant-guard, validation, and logging decorator pipeline.
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, TResponse, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(
            new ServiceDescriptor(
                typeof(ICommandHandler<TCommand, TResponse>),
                static serviceProvider =>
                {
                    var inner = serviceProvider.GetRequiredService<THandler>();
                    var validators = serviceProvider.GetServices<IRequestValidator<TCommand>>();
                    var validating = new ValidatingCommandHandler<TCommand, TResponse>(
                        inner,
                        validators
                    );
                    var guarded = new TenantGuardCommandHandler<TCommand, TResponse>(
                        validating,
                        serviceProvider.GetRequiredService<ITenantContext>(),
                        serviceProvider.GetService<IPrincipalTenantValidator>()
                    );
                    var logger = serviceProvider.GetRequiredService<
                        ILogger<LoggingCommandHandler<TCommand, TResponse>>
                    >();
                    var clock = serviceProvider.GetRequiredService<IClock>();

                    return new LoggingCommandHandler<TCommand, TResponse>(guarded, logger, clock);
                },
                lifetime
            )
        );

        return services;
    }

    /// <summary>
    /// Explicitly registers a query handler wrapped in the tenant-guard, validation, and logging decorator pipeline.
    /// </summary>
    public static IServiceCollection AddQueryHandler<TQuery, TResponse, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));
        services.Add(
            new ServiceDescriptor(
                typeof(IQueryHandler<TQuery, TResponse>),
                static serviceProvider =>
                {
                    var inner = serviceProvider.GetRequiredService<THandler>();
                    var validators = serviceProvider.GetServices<IRequestValidator<TQuery>>();
                    var validating = new ValidatingQueryHandler<TQuery, TResponse>(
                        inner,
                        validators
                    );
                    var guarded = new TenantGuardQueryHandler<TQuery, TResponse>(
                        validating,
                        serviceProvider.GetRequiredService<ITenantContext>(),
                        serviceProvider.GetService<IPrincipalTenantValidator>()
                    );
                    var logger = serviceProvider.GetRequiredService<
                        ILogger<LoggingQueryHandler<TQuery, TResponse>>
                    >();
                    var clock = serviceProvider.GetRequiredService<IClock>();

                    return new LoggingQueryHandler<TQuery, TResponse>(guarded, logger, clock);
                },
                lifetime
            )
        );

        return services;
    }

    /// <summary>Registers a validator for a command or query.</summary>
    public static IServiceCollection AddRequestValidator<TRequest, TValidator>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TValidator : class, IRequestValidator<TRequest>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(
            new ServiceDescriptor(typeof(IRequestValidator<TRequest>), typeof(TValidator), lifetime)
        );

        return services;
    }

    /// <summary>Registers a consumer for an in-process <see cref="IDomainEvent" />.</summary>
    public static IServiceCollection AddDomainEventConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TEvent : IDomainEvent
        where TConsumer : class, IDomainEventConsumer<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(
            new ServiceDescriptor(typeof(IDomainEventConsumer<TEvent>), typeof(TConsumer), lifetime)
        );

        return services;
    }

    /// <summary>Registers a consumer for a cross-module <see cref="IIntegrationEvent" />.</summary>
    public static IServiceCollection AddIntegrationEventConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Add(
            new ServiceDescriptor(
                typeof(IIntegrationEventConsumer<TEvent>),
                typeof(TConsumer),
                lifetime
            )
        );

        return services;
    }
}
