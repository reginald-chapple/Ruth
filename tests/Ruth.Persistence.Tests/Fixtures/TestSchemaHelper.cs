using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Persistence.EntityFramework;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Persistence.Tests.Fixtures;

internal static class TestSchemaHelper
{
    public static async Task EnsureSchemaTablesCreatedAsync(DbContext dbContext, string schema)
    {
        var validated = SchemaValidator.Validate(schema);
        if (AmbientTenantContext.CurrentTenantId.IsEmpty)
        {
            AmbientTenantContext.SetTestHarnessDefault(Postgres18ContainerFixture.DefaultTenantId);
        }

        var appServices = dbContext
            .GetService<IDbContextOptions>()
            ?.FindExtension<CoreOptionsExtension>()
            ?.ApplicationServiceProvider;
        var provisioner = appServices?.GetService<PostgresRlsRoleProvisioner>();

        if (
            provisioner is not null
            && dbContext is ModuleDbContext { RequiresTenantIsolation: true }
        )
        {
            await ProvisionAndCreateTenantTablesAsync(dbContext, provisioner, validated);
            return;
        }

        await CreateTablesDirectAsync(dbContext, validated);
    }

    private static async Task ProvisionAndCreateTenantTablesAsync(
        DbContext dbContext,
        PostgresRlsRoleProvisioner provisioner,
        string validatedSchema
    )
    {
        await provisioner.EnsureAppRoleAndSchemaGrantsAsync(validatedSchema);

        var contextType = dbContext.GetType();
        var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType)!;
        _ = optionsBuilder.UseNpgsql(provisioner.AdminConnectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", validatedSchema)
        );

        await using (
            var adminContext = (DbContext)
                Activator.CreateInstance(contextType, optionsBuilder.Options)!
        )
        {
            await CreateTablesDirectAsync(adminContext, validatedSchema);
        }

        await provisioner.EnsureAppRoleAndSchemaGrantsAsync(validatedSchema);
        await provisioner.EnableSchemaRowLevelSecurityAsync(validatedSchema);
    }

    private static async Task CreateTablesDirectAsync(DbContext dbContext, string validatedSchema)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await dbContext.Database.OpenConnectionAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{validatedSchema}\";";
            _ = await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await dbContext.Database.CloseConnectionAsync();
            }
        }

        var creator = dbContext.GetService<IRelationalDatabaseCreator>();
        try
        {
            await creator.CreateTablesAsync();
        }
        catch (PostgresException ex)
            when (string.Equals(ex.SqlState, "42P07", StringComparison.Ordinal))
        {
            // Tables already exist in this test database schema.
        }
    }
}
