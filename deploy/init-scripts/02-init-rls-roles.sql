DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ruth_migrator') THEN
        CREATE ROLE ruth_migrator LOGIN PASSWORD 'ruth_migrator_local_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    ELSE
        ALTER ROLE ruth_migrator WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ruth_app') THEN
        CREATE ROLE ruth_app LOGIN PASSWORD 'ruth_app_rls_local_dev_pw' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    ELSE
        ALTER ROLE ruth_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;

    EXECUTE format('GRANT CONNECT, CREATE ON DATABASE %I TO ruth_migrator;', current_database());
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO ruth_app;', current_database());
    GRANT USAGE, CREATE ON SCHEMA public TO ruth_migrator;
    GRANT USAGE ON SCHEMA public TO ruth_app;
END
$$;
