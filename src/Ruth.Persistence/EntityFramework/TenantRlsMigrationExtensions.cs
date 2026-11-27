using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Persistence.DependencyInjection;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Migration and startup helpers for applying PostgreSQL 18 FORCE ROW LEVEL SECURITY policies
/// and running module DDL migrations under the schema-owner connection before granting ruth_app DML rights.
/// </summary>
public static class TenantRlsMigrationExtensions
{
    /// <summary>
    /// Emits an idempotent PostgreSQL PL/pgSQL block enabling ENABLE ROW LEVEL SECURITY,
    /// FORCE ROW LEVEL SECURITY, and both permissive and restrictive tenant_isolation_policy
    /// guards on every table containing a tenant_id column in <paramref name="schema" />.
    /// </summary>
    public static void EnableTenantRowLevelSecurity(
        this MigrationBuilder migrationBuilder,
        string schema
    )
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);
        var validatedSchema = SchemaValidator.Validate(schema);

        _ = migrationBuilder.Sql(BuildEnableTenantRlsSql(validatedSchema));
    }

    /// <summary>Builds the idempotent SQL script enabling FORCE ROW LEVEL SECURITY on <paramref name="validatedSchema" />.</summary>
    public static string BuildEnableTenantRlsSql(string validatedSchema)
    {
        return PostgresRlsSqlScripts.BuildStaticSchemaRlsDoBlock(validatedSchema);
    }

    /// <summary>
    /// Runs EF Core migrations for <typeparamref name="TContext" /> using the schema-owner migrator connection,
    /// then provisions ruth_app DML grants and verifies FORCE ROW LEVEL SECURITY on tenant schemas
    /// (or locks down control-plane schemas against ruth_app access).
    /// </summary>
    public static async Task MigrateModuleSchemaAsync<TContext>(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default
    )
        where TContext : ModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var descriptor = serviceProvider.GetService<ModuleMigrationDescriptor<TContext>>();
        var provisioner = serviceProvider.GetService<PostgresRlsRoleProvisioner>();
        if (descriptor is null || provisioner is null)
        {
            await using var fallbackScope = serviceProvider.CreateAsyncScope();
            var fallbackContext = fallbackScope.ServiceProvider.GetRequiredService<TContext>();
            await fallbackContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (descriptor.Options.IsTenantScopedSchema)
        {
            await provisioner.EnsureAppRoleAndSchemaGrantsAsync(
                descriptor.Schema,
                cancellationToken
            );
        }

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(descriptor.AdminConnectionString);
        descriptor.Options.ConfigureDataSource?.Invoke(dataSourceBuilder);
        await using (var adminDataSource = dataSourceBuilder.Build())
        {
            var optionsBuilder = new DbContextOptionsBuilder<TContext>();
            _ = optionsBuilder.UseNpgsql(adminDataSource, npgsql =>
            {
                _ = npgsql.MigrationsHistoryTable("__EFMigrationsHistory", descriptor.Schema);
                descriptor.Options.ConfigureNpgsql?.Invoke(npgsql);
            });
            descriptor.Options.ConfigureDbContext?.Invoke(optionsBuilder);

            await using var scope = serviceProvider.CreateAsyncScope();
            await using var adminContext = ActivatorUtilities.CreateInstance<TContext>(
                scope.ServiceProvider,
                optionsBuilder.Options
            );
            await adminContext.Database.MigrateAsync(cancellationToken);
        }

        if (descriptor.Options.IsTenantScopedSchema)
        {
            await provisioner.EnsureAppRoleAndSchemaGrantsAsync(
                descriptor.Schema,
                cancellationToken
            );
            await provisioner.EnableSchemaRowLevelSecurityAsync(
                descriptor.Schema,
                cancellationToken
            );
        }
        else
        {
            await provisioner.LockDownControlPlaneSchemaAsync(descriptor.Schema, cancellationToken);
        }
    }
}
