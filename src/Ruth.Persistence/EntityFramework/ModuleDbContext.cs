using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.Outbox;
using Ruth.Persistence.Outbox.Configurations;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Base EF Core <see cref="DbContext" /> for bounded-context modules with schema isolation,
/// multi-tenant Global Query Filters + PostgreSQL 18 RLS integration, UTC timestamp conventions,
/// strongly-typed ID conversion, and transactional Outbox/Inbox tables.
/// </summary>
/// <param name="options">The EF Core options configured for the module context.</param>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    private static readonly MethodInfo _applyTenantFilterMethod = typeof(ModuleDbContext).GetMethod(
        nameof(ApplyTenantQueryFilter),
        BindingFlags.Instance | BindingFlags.NonPublic
    )!;

    /// <summary>Gets the PostgreSQL schema owned exclusively by this bounded-context module.</summary>
    protected abstract string Schema { get; }

    /// <summary>Gets whether this module schema contains tenant-scoped data protected by RLS and Global Query Filters.</summary>
    protected virtual bool IsTenantScopedSchema => true;

    internal string ModuleSchema => Schema;

    internal bool RequiresTenantIsolation => IsTenantScopedSchema;

    /// <summary>Gets the active <see cref="TenantId" /> evaluated dynamically per EF Core query.</summary>
    public TenantId CurrentTenantId =>
        IsTenantScopedSchema ? AmbientTenantContext.CurrentTenantId : TenantId.Empty;

    /// <summary>Gets the assembly scanned for <see cref="IEntityTypeConfiguration{TEntity}" /> and strongly-typed IDs.</summary>
    protected virtual Assembly ConfigurationsAssembly => GetType().Assembly;

    /// <summary>Gets the transactional Outbox messages table in this module's schema.</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>Gets the idempotent Inbox deduplication table in this module's schema.</summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <summary>Stages an <see cref="IIntegrationEvent" /> into the module's Outbox within the current transaction.</summary>
    /// <param name="integrationEvent">The integration event to publish atomically on commit.</param>
    public void EnqueueIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        _ = StageIntegrationEvent(integrationEvent);
    }

    internal OutboxMessage StageIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var registry = this.GetService<IDbContextOptions>()
            ?.FindExtension<CoreOptionsExtension>()
            ?.ApplicationServiceProvider?.GetService<OutboxEventTypeRegistry>();

        var eventType = integrationEvent.GetType();
        var eventTypeName = registry is not null
            ? registry.GetEventTypeName(eventType)
            : OutboxEventTypeRegistry.GetCanonicalTypeName(eventType);

        var payload = OutboxJsonSerializer.Serialize(integrationEvent);
        var message = OutboxMessage.Create(
            integrationEvent.EventId,
            eventTypeName,
            payload,
            integrationEvent.OccurredOnUtc
        );

        if (IsTenantScopedSchema && !CurrentTenantId.IsEmpty)
        {
            message.AssignTenant(CurrentTenantId);
        }

        _ = OutboxMessages.Add(message);
        return message;
    }

    /// <inheritdoc />
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var validatedSchema = SchemaValidator.Validate(Schema);
        _ = modelBuilder.HasDefaultSchema(validatedSchema);
        _ = modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(IsTenantScopedSchema));
        _ = modelBuilder.ApplyConfiguration(new InboxMessageConfiguration(IsTenantScopedSchema));

        if (ConfigurationsAssembly != typeof(ModuleDbContext).Assembly)
        {
            _ = modelBuilder.ApplyConfigurationsFromAssembly(ConfigurationsAssembly);
        }

        OnModuleModelCreating(modelBuilder);
        RuthModelBuilderConventions.ConfigureConcurrencyTokens(modelBuilder);
        RuthModelBuilderConventions.ConfigureTenantIsolation(
            modelBuilder,
            this,
            IsTenantScopedSchema
        );
    }

    /// <summary>Configures module-specific entity mappings after base Outbox/Inbox configuration.</summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    protected virtual void OnModuleModelCreating(ModelBuilder modelBuilder) { }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        RuthModelBuilderConventions.ConfigureConventions(
            configurationBuilder,
            ConfigurationsAssembly
        );
    }

    /// <summary>Configures PostgreSQL xmin system-column optimistic concurrency for <typeparamref name="TEntity" />.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The entity type builder.</param>
    protected static void ConfigurePostgresRowVersion<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        RuthModelBuilderConventions.ConfigurePostgresRowVersion(builder);
    }

    internal void ApplyTenantQueryFilterForClrType(ModelBuilder modelBuilder, Type entityClrType)
    {
        _ = _applyTenantFilterMethod.MakeGenericMethod(entityClrType).Invoke(this, [modelBuilder]);
    }

    private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        _ = modelBuilder
            .Entity<TEntity>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
    }
}
