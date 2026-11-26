using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.DependencyInjection;

/// <summary>Captures migration-time connection and schema metadata for <typeparamref name="TContext" />.</summary>
internal sealed record ModuleMigrationDescriptor<TContext>(
    string AdminConnectionString,
    string Schema,
    ModuleDbContextRegistrationOptions Options
)
    where TContext : ModuleDbContext;
