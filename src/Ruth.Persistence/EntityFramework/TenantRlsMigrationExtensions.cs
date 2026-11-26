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
        var schema = SchemaValidator.Validate(validatedSchema);
        return string.Join(
            '\n',
            "DO $$",
            "DECLARE",
            "    tbl record;",
            "BEGIN",
            "    FOR tbl IN",
            "        SELECT c.relname AS table_name",
            "        FROM pg_class c",
            "        JOIN pg_namespace n ON n.oid = c.relnamespace",
            "        JOIN pg_attribute a ON a.attrelid = c.oid",
            $"        WHERE n.nspname = '{schema}'",
            "          AND c.relkind IN ('r', 'p')",
            "          AND a.attname = 'tenant_id'",
            "          AND NOT a.attisdropped",
            "    LOOP",
            $"        EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY;', '{schema}', tbl.table_name);",
            $"        EXECUTE format('ALTER TABLE %I.%I FORCE ROW LEVEL SECURITY;', '{schema}', tbl.table_name);",
            $"        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_policy ON %I.%I;', '{schema}', tbl.table_name);",
            $"        EXECUTE format('DROP POLICY IF EXISTS tenant_restrictive_guard ON %I.%I;', '{schema}', tbl.table_name);",
            $"        EXECUTE format('CREATE POLICY tenant_isolation_policy ON %I.%I AS PERMISSIVE FOR ALL USING (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid) WITH CHECK (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid);', '{schema}', tbl.table_name);",
            $"        EXECUTE format('CREATE POLICY tenant_restrictive_guard ON %I.%I AS RESTRICTIVE FOR ALL USING (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid) WITH CHECK (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid);', '{schema}', tbl.table_name);",
            "    END LOOP;",
            "END",
            "$$;"
        );
    }

    /// <summary>
    /// Runs EF Core migrations for <typeparamref name="TContext" /> using the schema-owner migrator connection,
    /// then provisions ruth_app DML grants and verifies FORCE ROW LEVEL SECURITY on tenant schemas.
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
    }
}
