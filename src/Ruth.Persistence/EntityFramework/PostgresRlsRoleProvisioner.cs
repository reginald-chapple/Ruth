using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Ruth.Persistence.EntityFramework;

/// <summary>
/// Provisions the non-superuser ruth_app PostgreSQL runtime role (NOBYPASSRLS), grants DML-only
/// schema privileges, and applies FORCE ROW LEVEL SECURITY policies on tenant schemas.
/// </summary>
public sealed class PostgresRlsRoleProvisioner
{
    private const string SetProvisionConfigSql =
        "SELECT pg_advisory_xact_lock(788475718), "
        + "set_config('ruth.target_schema', @schema, true), "
        + "set_config('ruth.app_role', @role, true), "
        + "set_config('ruth.app_password', @password, true);";

    private static readonly string _provisionRoleAndGrantsDoBlockSql = string.Join(
        '\n',
        "DO $$",
        "DECLARE",
        "    v_schema text := current_setting('ruth.target_schema');",
        "    v_role text := current_setting('ruth.app_role');",
        "    v_password text := current_setting('ruth.app_password');",
        "BEGIN",
        "    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN",
        "        EXECUTE format(",
        "            'CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;',",
        "            v_role,",
        "            v_password",
        "        );",
        "    ELSE",
        "        EXECUTE format(",
        "            'ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;',",
        "            v_role,",
        "            v_password",
        "        );",
        "    END IF;",
        "    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I;', current_database(), v_role);",
        "    EXECUTE format('GRANT USAGE ON SCHEMA public TO %I;', v_role);",
        "    IF EXISTS (",
        "        SELECT 1",
        "        FROM pg_class c",
        "        JOIN pg_namespace n ON n.oid = c.relnamespace",
        "        WHERE n.nspname = 'public' AND c.relname = 'spatial_ref_sys'",
        "    ) THEN",
        "        EXECUTE format('GRANT SELECT ON TABLE public.spatial_ref_sys TO %I;', v_role);",
        "    END IF;",
        "    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I;', v_schema);",
        "    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I;', v_schema, v_role);",
        "    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO %I;', v_schema, v_role);",
        "END",
        "$$;"
    );

    private const string SetRlsSchemaConfigSql =
        "SELECT pg_advisory_xact_lock(788475719), set_config('ruth.target_schema', @schema, true);";

    private static readonly string _enableSchemaRlsDoBlockSql = string.Join(
        '\n',
        "DO $$",
        "DECLARE",
        "    v_schema text := current_setting('ruth.target_schema');",
        "    tbl record;",
        "BEGIN",
        "    FOR tbl IN",
        "        SELECT c.relname AS table_name",
        "        FROM pg_class c",
        "        JOIN pg_namespace n ON n.oid = c.relnamespace",
        "        JOIN pg_attribute a ON a.attrelid = c.oid",
        "        WHERE n.nspname = v_schema",
        "          AND c.relkind IN ('r', 'p')",
        "          AND a.attname = 'tenant_id'",
        "          AND NOT a.attisdropped",
        "    LOOP",
        "        EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY;', v_schema, tbl.table_name);",
        "        EXECUTE format('ALTER TABLE %I.%I FORCE ROW LEVEL SECURITY;', v_schema, tbl.table_name);",
        "        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation_policy ON %I.%I;', v_schema, tbl.table_name);",
        "        EXECUTE format('DROP POLICY IF EXISTS tenant_restrictive_guard ON %I.%I;', v_schema, tbl.table_name);",
        "        EXECUTE format(",
        "            'CREATE POLICY tenant_isolation_policy ON %I.%I AS PERMISSIVE FOR ALL USING (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid) WITH CHECK (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid);',",
        "            v_schema,",
        "            tbl.table_name",
        "        );",
        "        EXECUTE format(",
        "            'CREATE POLICY tenant_restrictive_guard ON %I.%I AS RESTRICTIVE FOR ALL USING (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid) WITH CHECK (tenant_id = NULLIF(current_setting(''app.current_tenant_id'', true), '''')::uuid);',",
        "            v_schema,",
        "            tbl.table_name",
        "        );",
        "    END LOOP;",
        "END",
        "$$;"
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

    /// <summary>Builds the connection string used by tenant-scoped runtime <see cref="ModuleDbContext" /> instances.</summary>
    public string BuildTenantRuntimeConnectionString(string baseConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseConnectionString);

        if (!Options.UseNonSuperuserAppRole)
        {
            return baseConnectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Username = Options.AppRoleName,
            Password = Options.AppRolePassword,
        };
        return builder.ConnectionString;
    }

    /// <summary>Ensures the ruth_app role exists with NOBYPASSRLS and grants DML privileges on <paramref name="schema" />.</summary>
    public async Task EnsureAppRoleAndSchemaGrantsAsync(
        string schema,
        CancellationToken cancellationToken = default
    )
    {
        var validatedSchema = SchemaValidator.Validate(schema);
        var validatedRole = SchemaValidator.Validate(Options.AppRoleName);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var configCommand = connection.CreateCommand())
        {
            configCommand.Transaction = transaction;
            configCommand.CommandText = SetProvisionConfigSql;
            _ = configCommand.Parameters.Add(
                new NpgsqlParameter("schema", NpgsqlDbType.Text) { Value = validatedSchema }
            );
            _ = configCommand.Parameters.Add(
                new NpgsqlParameter("role", NpgsqlDbType.Text) { Value = validatedRole }
            );
            _ = configCommand.Parameters.Add(
                new NpgsqlParameter("password", NpgsqlDbType.Text)
                {
                    Value = Options.AppRolePassword,
                }
            );
            _ = await configCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var ddlCommand = connection.CreateCommand())
        {
            ddlCommand.Transaction = transaction;
            ddlCommand.CommandText = _provisionRoleAndGrantsDoBlockSql;
            _ = await ddlCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Applies ENABLE and FORCE ROW LEVEL SECURITY and tenant_isolation_policy across all tenant_id tables in <paramref name="schema" />.</summary>
    public async Task EnableSchemaRowLevelSecurityAsync(
        string schema,
        CancellationToken cancellationToken = default
    )
    {
        var validatedSchema = SchemaValidator.Validate(schema);

        await using var connection = new NpgsqlConnection(AdminConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var configCommand = connection.CreateCommand())
        {
            configCommand.Transaction = transaction;
            configCommand.CommandText = SetRlsSchemaConfigSql;
            _ = configCommand.Parameters.Add(
                new NpgsqlParameter("schema", NpgsqlDbType.Text) { Value = validatedSchema }
            );
            _ = await configCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var rlsCommand = connection.CreateCommand())
        {
            rlsCommand.Transaction = transaction;
            rlsCommand.CommandText = _enableSchemaRlsDoBlockSql;
            _ = await rlsCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
