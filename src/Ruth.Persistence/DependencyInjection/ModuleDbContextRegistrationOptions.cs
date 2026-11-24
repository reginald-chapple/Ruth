using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Ruth.Persistence.DependencyInjection;

/// <summary>
/// Per-module configuration callbacks for AddModuleDbContext, supporting PostgreSQL 18 extensions
/// (postgis, pgvector, Apache age) and custom EF Core options.
/// </summary>
public sealed class ModuleDbContextRegistrationOptions
{
    /// <summary>Gets or sets an optional callback to configure a module-dedicated <see cref="NpgsqlDataSourceBuilder" />.</summary>
    public Action<NpgsqlDataSourceBuilder>? ConfigureDataSource { get; set; }

    /// <summary>Gets or sets an optional callback to configure <see cref="NpgsqlDbContextOptionsBuilder" />.</summary>
    public Action<NpgsqlDbContextOptionsBuilder>? ConfigureNpgsql { get; set; }

    /// <summary>Gets or sets an optional callback to configure <see cref="DbContextOptionsBuilder" />.</summary>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
}
