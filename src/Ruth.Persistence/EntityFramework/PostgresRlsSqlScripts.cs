namespace Ruth.Persistence.EntityFramework;

internal static class PostgresRlsSqlScripts
{
    public const string SetProvisionConfigSql =
        "SELECT pg_advisory_xact_lock(788475718), "
        + "set_config('ruth.target_schema', @schema, true), "
        + "set_config('ruth.migrator_role', @migrator_role, true), "
        + "set_config('ruth.migrator_password', @migrator_password, true), "
        + "set_config('ruth.app_role', @role, true), "
        + "set_config('ruth.app_password', @password, true);";

    public const string SetSchemaAndAppRoleConfigSql =
        "SELECT pg_advisory_xact_lock(788475719), "
        + "set_config('ruth.target_schema', @schema, true), "
        + "set_config('ruth.app_role', @role, true);";

    public static readonly string ProvisionRolesAndTenantSchemaGrantsDoBlockSql = string.Join(
        '\n',
        "DO $$",
        "DECLARE",
        "    v_schema text := current_setting('ruth.target_schema');",
        "    v_migrator text := current_setting('ruth.migrator_role');",
        "    v_migrator_pw text := current_setting('ruth.migrator_password');",
        "    v_role text := current_setting('ruth.app_role');",
        "    v_password text := current_setting('ruth.app_password');",
        "    v_can_manage_roles boolean := false;",
        "    v_is_super boolean := false;",
        "    v_is_bypass boolean := false;",
        "BEGIN",
        "    SELECT (rolsuper OR rolcreaterole) INTO v_can_manage_roles FROM pg_roles WHERE rolname = current_user;",
        "    IF v_can_manage_roles THEN",
        "        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_migrator) THEN",
        "            EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;', v_migrator, v_migrator_pw);",
        "        ELSE",
        "            EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;', v_migrator, v_migrator_pw);",
        "        END IF;",
        "        EXECUTE format('GRANT CONNECT, CREATE ON DATABASE %I TO %I;', current_database(), v_migrator);",
        "        EXECUTE format('GRANT USAGE, CREATE ON SCHEMA public TO %I;', v_migrator);",
        "        IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN",
        "            EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;', v_role, v_password);",
        "        ELSE",
        "            EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;', v_role, v_password);",
        "        END IF;",
        "        EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I;', current_database(), v_role);",
        "        EXECUTE format('GRANT USAGE ON SCHEMA public TO %I;', v_role);",
        "        IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relname = 'spatial_ref_sys') THEN",
        "            EXECUTE format('GRANT SELECT ON TABLE public.spatial_ref_sys TO %I;', v_role);",
        "        END IF;",
        "    END IF;",
        "    SELECT rolsuper, rolbypassrls INTO v_is_super, v_is_bypass FROM pg_roles WHERE rolname = v_role;",
        "    IF NOT FOUND THEN",
        "        RAISE EXCEPTION 'Required runtime RLS role % does not exist.', v_role;",
        "    END IF;",
        "    IF v_is_super OR v_is_bypass THEN",
        "        RAISE EXCEPTION 'Runtime role % must be NOSUPERUSER and NOBYPASSRLS.', v_role;",
        "    END IF;",
        "    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I;', v_schema);",
        "    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_migrator) AND v_can_manage_roles THEN",
        "        EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA %I TO %I;', v_schema, v_migrator);",
        "    END IF;",
        "    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO %I;', v_schema, v_role);",
        "    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I;', v_schema, v_role);",
        "    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO %I;', v_schema, v_role);",
        "    IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = v_schema AND c.relname = '__EFMigrationsHistory') THEN",
        "        EXECUTE format('REVOKE ALL ON TABLE %I.\"__EFMigrationsHistory\" FROM PUBLIC, %I;', v_schema, v_role);",
        "    END IF;",
        "END",
        "$$;"
    );

    public static readonly string LockDownControlPlaneSchemaDoBlockSql = string.Join(
        '\n',
        "DO $$",
        "DECLARE",
        "    v_schema text := current_setting('ruth.target_schema');",
        "    v_role text := current_setting('ruth.app_role');",
        "BEGIN",
        "    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = v_schema) THEN",
        "        EXECUTE format('REVOKE ALL ON SCHEMA %I FROM PUBLIC;', v_schema);",
        "        EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM PUBLIC;', v_schema);",
        "        EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM PUBLIC;', v_schema);",
        "        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = v_role) THEN",
        "            EXECUTE format('REVOKE ALL ON SCHEMA %I FROM %I;', v_schema, v_role);",
        "            EXECUTE format('REVOKE ALL ON ALL TABLES IN SCHEMA %I FROM %I;', v_schema, v_role);",
        "            EXECUTE format('REVOKE ALL ON ALL SEQUENCES IN SCHEMA %I FROM %I;', v_schema, v_role);",
        "            EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I REVOKE ALL ON TABLES FROM PUBLIC, %I;', v_schema, v_role);",
        "            EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I REVOKE ALL ON SEQUENCES FROM PUBLIC, %I;', v_schema, v_role);",
        "        END IF;",
        "    END IF;",
        "END",
        "$$;"
    );

    public static readonly string EnableSchemaRlsDoBlockSql = BuildDynamicEnableSchemaRlsDoBlock(
        "current_setting('ruth.target_schema')"
    );

    public static string BuildStaticSchemaRlsDoBlock(string validatedSchema)
    {
        var schema = SchemaValidator.Validate(validatedSchema);
        return BuildDynamicEnableSchemaRlsDoBlock($"'{schema}'");
    }

    private static string BuildDynamicEnableSchemaRlsDoBlock(string schemaSqlExpr)
    {
        return string.Join(
            '\n',
            "DO $$",
            "DECLARE",
            $"    v_schema text := {schemaSqlExpr};",
            "    v_missing_table text;",
            "    tbl record;",
            "BEGIN",
            "    SELECT c.relname INTO v_missing_table",
            "    FROM pg_class c",
            "    JOIN pg_namespace n ON n.oid = c.relnamespace",
            "    WHERE n.nspname = v_schema",
            "      AND c.relkind IN ('r', 'p')",
            "      AND c.relname <> '__EFMigrationsHistory'",
            "      AND NOT EXISTS (",
            "          SELECT 1 FROM pg_attribute a",
            "          WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND NOT a.attisdropped",
            "      )",
            "    LIMIT 1;",
            "    IF v_missing_table IS NOT NULL THEN",
            "        RAISE EXCEPTION 'Tenant table %.% is missing mandatory tenant_id column required for RLS.', v_schema, v_missing_table;",
            "    END IF;",
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
    }
}
