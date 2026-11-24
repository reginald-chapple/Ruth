using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.Outbox;

/// <summary>Schema descriptor binding a module <see cref="ModuleDbContext" /> type to its PostgreSQL schema.</summary>
/// <typeparam name="TContext">The module <see cref="ModuleDbContext" /> type.</typeparam>
public sealed record OutboxSchemaDescriptor<TContext>(string Schema)
    where TContext : ModuleDbContext;
