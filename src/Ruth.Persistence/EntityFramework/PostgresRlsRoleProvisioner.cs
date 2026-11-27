using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Provisions the non-superuser ruth_migrator (DDL/control-plane) and ruth_app (NOBYPASSRLS DML)
/// PostgreSQL roles, revokes runtime access to control-plane and migration-history tables, and enforces
/// FORCE ROW LEVEL SECURITY policies on tenant schemas.
/// </summary>
public sealed class PostgresRlsRoleProvisioner
{
    private readonly ConcurrentDictionary<string, string> _runtimeConnCache = new(
        StringComparer.Ordinal
    );

    /// <summary>Initializes a new <see cref="PostgresRlsRoleProvisioner" />.</summary>
    public PostgresRlsRoleProvisioner(string adminConnectionString, IOptions<RlsOptions> options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminConnectionString);
        ArgumentNullException.ThrowIfNull(options);

        AdminConnectionString = adminConnectionString;
        Options = options.Value;
    }

    /// <summary>Gets the schema-owner / migrator connection string.</summary>
    public string AdminConnectionString { get; }

    /// <summary>Gets the configured RLS options.</summary>
    public RlsOptions Options { get; }

    /// <summary>Builds and caches the connection string used by tenant-scoped runtime <see cref="ModuleDbContext" /> instances.</summary>
    public string BuildTenantRuntimeConnectionString(string baseConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseConnectionString);

        return !Options.UseNonSuperuserAppRole
            ? baseConnectionString
            : _runtimeConnCache.GetOrAdd(
                baseConnectionString,
                static (rawConn, opts) =>
                {
                    var builder = new NpgsqlConnectionStringBuilder(rawConn)
                    {
                        Username = opts.AppRoleName,
                        Password = opts.AppRolePassword,
                    };
                    return builder.ConnectionString;
                },
                Options
            );
    }

    /// <summary>Builds a connection string authenticating as the non-superuser ruth_migrator DDL role.</summary>
    public string BuildMigratorConnectionString(string? baseConnectionString = null)
    {
        var source = string.IsNullOrWhiteSpace(baseConnectionString)
            ? AdminConnectionString
            : baseConnectionString;
        var builder = new NpgsqlConnectionStringBuilder(source)
        {
            Username = Options.MigratorRoleName,
            Password = Options.MigratorRolePassword,
        };
        return builder.ConnectionString;
    }

    /// <summary>Ensures ruth_migrator and ruth_app exist with NOBYPASSRLS and grants DML-only privileges on <paramref name="schema" />.</summary>
    public async Task EnsureAppRoleAndSchemaGrantsAsync(
        string schema,
        CancellationToken cancellationToken = default
    )
    {
        var validatedSchema = SchemaValidator.Validate(schema);
        var validatedMigrator = SchemaValidator.Validate(Options.MigratorRoleName);
        var validatedRole = SchemaValidator.Validate(Options.AppRoleName);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var configCommand = connection.CreateCommand())
        {
            configCommand.Transaction = transaction;
            configCommand.CommandText = PostgresRlsSqlScripts.SetProvisionConfigSql;
            AddTextParam(configCommand, "schema", validatedSchema);
            AddTextParam(configCommand, "migrator_role", validatedMigrator);
            AddTextParam(configCommand, "migrator_password", Options.MigratorRolePassword);
            AddTextParam(configCommand, "role", validatedRole);
            AddTextParam(configCommand, "password", Options.AppRolePassword);
            _ = await configCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var ddlCommand = connection.CreateCommand())
        {
            ddlCommand.Transaction = transaction;
            ddlCommand.CommandText =
                PostgresRlsSqlScripts.ProvisionRolesAndTenantSchemaGrantsDoBlockSql;
            _ = await ddlCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Revokes all PUBLIC and ruth_app privileges on non-tenant control-plane schemas (platform, scheduler).</summary>
    public async Task LockDownControlPlaneSchemaAsync(
        string schema,
        CancellationToken cancellationToken = default
    )
    {
        var validatedSchema = SchemaValidator.Validate(schema);
        var validatedRole = SchemaValidator.Validate(Options.AppRoleName);

        await ExecuteSchemaDoBlockAsync(
            validatedSchema,
            validatedRole,
            PostgresRlsSqlScripts.LockDownControlPlaneSchemaDoBlockSql,
            cancellationToken
        );
    }

    /// <summary>Applies ENABLE and FORCE ROW LEVEL SECURITY and tenant_isolation_policy across all tenant_id tables in <paramref name="schema" />.</summary>
    public async Task EnableSchemaRowLevelSecurityAsync(
        string schema,
        CancellationToken cancellationToken = default
    )
    {
        var validatedSchema = SchemaValidator.Validate(schema);
        var validatedRole = SchemaValidator.Validate(Options.AppRoleName);

        await ExecuteSchemaDoBlockAsync(
            validatedSchema,
            validatedRole,
            PostgresRlsSqlScripts.EnableSchemaRlsDoBlockSql,
            cancellationToken
        );
    }

    private async Task ExecuteSchemaDoBlockAsync(
        string validatedSchema,
        string validatedRole,
        string doBlockSql,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var configCommand = connection.CreateCommand())
        {
            configCommand.Transaction = transaction;
            configCommand.CommandText = PostgresRlsSqlScripts.SetSchemaAndAppRoleConfigSql;
            AddTextParam(configCommand, "schema", validatedSchema);
            AddTextParam(configCommand, "role", validatedRole);
            _ = await configCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var rlsCommand = connection.CreateCommand())
        {
            rlsCommand.Transaction = transaction;
            rlsCommand.CommandText = doBlockSql;
            _ = await rlsCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void AddTextParam(NpgsqlCommand command, string name, string value)
    {
        _ = command.Parameters.Add(new NpgsqlParameter(name, NpgsqlDbType.Text) { Value = value });
    }
}
